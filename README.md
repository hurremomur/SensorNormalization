# Sensör Verisi Normalizasyonu

Farklı formatlarda (JSON, XML, CSV) ve farklı birimlerde veri üreten sensörlerden gelen
ham veriyi tek bir standart modele **normalize eden**, bir zaman serisi veritabanında
**saklayan** ve bir REST API üzerinden **raporlayan** uçtan uca bir veri işleme hattıdır.
Şirketin gerçek `Platform360.MES.TSDBConsumer` servisinin öğretici bir minyatürüdür.

Veri akışı: **Simülatör → RabbitMQ → Consumer (parse + normalize) → TimescaleDB → Reporting API**

---

## Normalizasyon Kuralları

| Sensör | Ham format | Ham birim/zaman | Normalize edilmiş |
|---|---|---|---|
| Sıcaklık (Temperature) | JSON | Fahrenheit, Unix time | Celsius (`C`), UTC |
| Nem (Humidity) | XML | Yüzde, +03:00 yerel saat | Yüzde (`%`), UTC |
| Basınç (Pressure) | CSV | mbar, UTC | hPa, UTC |

Tüm ölçümler tek bir `SensorReading` modeline ve UTC zaman eksenine indirgenir.

---

## Mimari

```
SensorNormalization.Domain/        Ortak model (entity, message, enum)
SensorNormalization.Application/    Ortak katman: DbContext + Service + Repository + Migrations
SensorNormalization.Consumer/       Yazma tarafi: RabbitMQ dinler, normalize eder, DB'ye yazar
SensorNormalization.Api/            Okuma tarafi: Reporting REST API (Swagger)
SensorNormalization.Simulator/      Sahte sensor verisi ureticisi
SensorNormalization.Tests/          Birim testleri (parser'lar)
```

`Consumer` (yazma) ve `Api` (okuma) ortak `Application` katmanını paylaşan ama ayrı çalışan
iki uygulamadır; böylece bağımsız olarak ölçeklenebilirler. Ayrıntılı gerekçe için
`docs/` altındaki mimari karar kaydına (ADR) bakınız.

---

## Teknolojiler

- **.NET 8** (C#)
- **MassTransit 8.3** + **RabbitMQ** — mesajlaşma
- **TimescaleDB** (PostgreSQL 16) — zaman serisi veritabanı (hypertable)
- **Entity Framework Core 8** + **Npgsql** — ORM
- **Docker Compose** — RabbitMQ, TimescaleDB, pgAdmin
- **xUnit** — birim testleri
- **Swagger / OpenAPI** — API dokümantasyonu

---

## Gereksinimler

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)

---

## Kurulum ve Çalıştırma

### 1. Altyapıyı başlat (Docker)

```bash
docker compose up -d
```

Bu komut üç konteyneri ayağa kaldırır:
- RabbitMQ (yönetim arayüzü: http://localhost:15672 — kullanıcı/şifre: `guest`/`guest`)
- TimescaleDB (port 5432)
- pgAdmin (http://localhost:5050 — `admin@admin.com`/`admin`)

### 2. Veritabanı şemasını uygula (yalnızca ilk kurulumda)

```bash
dotnet ef database update --project SensorNormalization.Application --startup-project SensorNormalization.Consumer
```

`sensor_readings` tablosu oluşur. Tabloyu hypertable'a çevirmek için (yalnızca ilk kurulumda):

```bash
docker exec sensor-timescaledb psql -U postgres -d sensordb -c "SELECT create_hypertable('sensor_readings', 'Time');"
```

### 3. Consumer'ı başlat (yazma tarafı)

```bash
dotnet run --project SensorNormalization.Consumer
```

### 4. Simülatörü başlat (veri üretimi) — ayrı terminal

```bash
dotnet run --project SensorNormalization.Simulator
```

Birkaç saniye içinde Consumer'da `Kaydedildi -> ...` logları akmaya başlar; veri
TimescaleDB'ye yazılır.

### 5. Reporting API'yi başlat — ayrı terminal

```bash
dotnet run --project SensorNormalization.Api
```

Tarayıcıda Swagger açılır: **http://localhost:5160/swagger**

---

## API Uç Noktaları

Temel yol: `http://localhost:5160/api/sensor-readings`
`{sensorType}` değerleri: `temperature`, `humidity`, `pressure`

### Tüm tiplerin son değeri

```bash
curl http://localhost:5160/api/sensor-readings/latest
```

### Belirli tipin son değeri

```bash
curl http://localhost:5160/api/sensor-readings/temperature/latest
```

### Sayfalı geçmiş (tarih aralığı opsiyonel)

```bash
curl "http://localhost:5160/api/sensor-readings/temperature/history?pageIndex=0&pageSize=10"
curl "http://localhost:5160/api/sensor-readings/pressure/history?from=2026-08-01T00:00:00Z&to=2026-08-05T00:00:00Z&pageIndex=0&pageSize=20"
```

Yanıt: `{ "pageIndex", "pageSize", "totalCount", "items": [...] }`

### İstatistik özeti (min / max / ortalama)

```bash
curl http://localhost:5160/api/sensor-readings/temperature/summary
curl "http://localhost:5160/api/sensor-readings/humidity/summary?from=2026-08-01T00:00:00Z&to=2026-08-05T00:00:00Z"
```

Yanıt: `{ "sensorType", "count", "min", "max", "average", "fromUtc", "toUtc" }`

### Hata modeli

- `400 Bad Request` — geçersiz `sensorType`, `from > to`, geçersiz sayfalama değeri
- `404 Not Found` — tip geçerli ama kayıt yok

---

## Testler

```bash
dotnet test
```

Parser'lar için birim testleri (normalizasyon doğruluğu + bozuk veri senaryoları) çalışır.

---

## Hata Dayanıklılığı

- **Bozuk veri (kalıcı hata):** parse edilemeyen mesaj loglanır ve atlanır; sistem durmaz.
- **Geçici hata (DB/ağ):** MassTransit `UseMessageRetry` ile artan aralıklarla yeniden denenir.
- **Kalıcı başarısızlık:** denemeler tükenirse mesaj otomatik `sensor-readings-queue_error`
  (dead-letter) kuyruğuna alınır; veri kaybolmaz, sonradan incelenebilir.

---

## Sayfalama Konvansiyonu

Tüm sistemde tek ve sabit konvansiyon kullanılır: **`pageIndex` (0-tabanlı) + `pageSize`**.
`offset/limit` ile karıştırılmaz. Geçersiz değerler `400` ile reddedilir.
