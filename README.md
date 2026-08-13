# Sensör Verisi Normalizasyonu

Farklı formatlarda (JSON, XML, CSV) ve farklı birimlerde veri üreten sensörlerden gelen
ham veriyi tek bir standart modele **normalize eden**, bir zaman serisi veritabanında
**saklayan**, bir REST API üzerinden **raporlayan** ve canlı bir panoda **izlenebilir kılan**
uçtan uca bir veri işleme hattıdır. Şirketin gerçek `Platform360.MES.TSDBConsumer`
servisinin öğretici bir minyatürüdür.

Veri akışı: **Simülatör → RabbitMQ → Consumer (parse + normalize) → TimescaleDB → Reporting API → Vue Panosu**

---

## Normalizasyon Kuralları

Sistem beş sensör tipini destekler. Her biri farklı format, birim ve zaman biçiminde gelir;
tümü tek bir `SensorReading` modeline ve UTC zaman eksenine indirgenir.

| Sensör | Ham format | Ham birim/zaman | Normalize edilmiş |
|---|---|---|---|
| Sıcaklık (Temperature) | JSON | Fahrenheit, Unix time | Celsius (`°C`), UTC |
| Nem (Humidity) | XML | Yüzde, +03:00 yerel saat | Yüzde (`%`), UTC |
| Basınç (Pressure) | CSV | mbar, UTC | hPa, UTC |
| Işık (Light) | JSON | lux, Unix time | lux, UTC |
| Ses (Sound) | JSON | dB, Unix time | dB, UTC |

Normalizasyon üç boyutta birlikte çalışır: **format** (JSON/XML/CSV → tek model),
**birim** (ör. `(F-32)*5/9`), **zaman** (Unix ve yerel saat → UTC).

---

## Öne Çıkan Özellikler

- **Üç eksenli normalizasyon** — format, birim ve zaman aynı anda tek standarda indirilir.
- **Otomatik format tespiti** — format bilgisi gelmese bile içerikten (`{`, `<`, ayraç) çıkarılır.
- **İstatistiksel anomali tespiti** — sabit eşik yerine, her sensörün son kayıtlarından
  ortalama ± 3σ hesaplanır; sistem her sensörün normalini veriden öğrenir.
- **Hata dayanıklılığı** — bozuk veri ayıklanır, geçici hatalar yeniden denenir,
  kalıcı hatalar dead-letter kuyruğuna alınır.
- **Genişletilebilirlik** — parser'lar reflection ile otomatik keşfedilir; yeni sensör
  eklerken DI, API hata mesajı ve arayüz kendiliğinden uyum sağlar (Açık/Kapalı ilkesi).
- **Canlı izleme panosu** — Vue + Vuetify ile Platform360 tarzı, kendini yenileyen arayüz.

---

## Mimari

```
SensorNormalization.Domain/         Ortak model (entity, message, enum)
SensorNormalization.Application/    Ortak katman: DbContext + Service + Repository + Migrations
SensorNormalization.Consumer/       Yazma tarafi: RabbitMQ dinler, normalize eder, DB'ye yazar
SensorNormalization.Api/            Okuma tarafi: Reporting REST API (Swagger)
SensorNormalization.Simulator/      Sahte sensor verisi ureticisi
SensorNormalization.Tests/          Birim + entegrasyon testleri
frontend/sensor-dashboard/          Vue 2 + Vuetify 2 canli izleme panosu
```

`Consumer` (yazma) ve `Api` (okuma) ortak `Application` katmanını paylaşan ama ayrı çalışan
iki uygulamadır; böylece bağımsız olarak ölçeklenebilirler.

### Tasarım desenleri

- **Strategy** — her format/tip için ayrı `ISensorPayloadParser`.
- **Factory** — gelen mesajın format + tip ikilisine göre doğru parser'ı seçer.
- **Repository + Dependency Injection** — veri erişimini soyutlar, testi kolaylaştırır.

---

## Teknolojiler

- **.NET 8** (C#)
- **Vue 2 + Vuetify 2** — canlı izleme panosu
- **MassTransit 8.3** + **RabbitMQ** — mesajlaşma
- **TimescaleDB** (PostgreSQL) — zaman serisi veritabanı (hypertable)
- **Entity Framework Core 8** + **Npgsql** — ORM
- **Docker Compose** — RabbitMQ, TimescaleDB, pgAdmin
- **xUnit** — birim + entegrasyon testleri
- **Swagger / OpenAPI** — API dokümantasyonu

---

## Gereksinimler

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Node.js](https://nodejs.org) (yalnızca panoyu çalıştırmak için)

---

## Kurulum ve Çalıştırma

### 1. Altyapıyı başlat (Docker)

```bash
docker compose up -d
```

Üç konteyner ayağa kalkar:
- RabbitMQ (yönetim arayüzü: http://localhost:15672 — `guest`/`guest`)
- TimescaleDB (port 5432)
- pgAdmin (http://localhost:5050 — `admin@admin.com`/`admin`)

### 2. Veritabanı şemasını uygula (yalnızca ilk kurulumda)

```bash
dotnet ef database update --project SensorNormalization.Application --startup-project SensorNormalization.Consumer
```

Tabloyu hypertable'a çevir (yalnızca ilk kurulumda):

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

Birkaç saniye içinde Consumer'da kayıt logları akar; veri TimescaleDB'ye yazılır.

### 5. Reporting API'yi başlat — ayrı terminal

```bash
dotnet run --project SensorNormalization.Api
```

Swagger: **http://localhost:5160/swagger**

### 6. İzleme panosunu başlat — ayrı terminal

```bash
cd frontend/sensor-dashboard
npm install
npm run serve
```

Pano: **http://localhost:8080** — beş sensör canlı akar, ham/normalize detayları ve
anomali göstergeleri görüntülenir.

---

## API Uç Noktaları

Temel yol: `http://localhost:5160/api/sensor-readings`
`{sensorType}` değerleri: `temperature`, `humidity`, `pressure`, `light`, `sound`

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
```

Yanıt: `{ "pageIndex", "pageSize", "totalCount", "items": [...] }`

### İstatistik özeti (min / max / ortalama)

```bash
curl http://localhost:5160/api/sensor-readings/temperature/summary
```

Yanıt: `{ "sensorType", "count", "min", "max", "average", "fromUtc", "toUtc" }`

### Hata modeli

- `400 Bad Request` — geçersiz `sensorType`, `from > to`, geçersiz sayfalama değeri.
  Geçersiz tipte, beklenen değerler listesi enum'dan otomatik üretilerek döndürülür.
- `404 Not Found` — tip geçerli ama kayıt yok.

---

## Testler

```bash
dotnet test
```

Kapsam: her parser için normalizasyon doğruluğu (F→C, +03:00→UTC, mbar→hPa) ve bozuk veri
senaryoları; format tespiti; parser factory seçimi; istatistiksel anomali (ortalama ± 3σ)
birim testleri; gerçek TimescaleDB'ye bağlanan entegrasyon testleri.

---

## Hata Dayanıklılığı

- **Bozuk veri (kalıcı hata):** parse edilemeyen mesaj için parser anlamlı bir hata fırlatır.
- **Geçici hata (DB/ağ):** MassTransit `UseMessageRetry` ile artan aralıklarla (1, 2, 5 sn)
  yeniden denenir.
- **Kalıcı başarısızlık:** denemeler tükenirse MassTransit mesajı otomatik olarak
  `sensor-readings-queue_error` (dead-letter) kuyruğuna alır; veri kaybolmaz.

Not: Retry açıkça yapılandırılmıştır; dead-letter kuyruğunu MassTransit, retry tanımlandığında
otomatik olarak sağlar.

---

## Genişletilebilirlik

Yeni bir sensör tipi eklemek, tasarım gereği yalnızca birkaç dosya değiştirir:

1. `SensorType` enum'una bir değer.
2. `ISensorPayloadParser` uygulayan bir parser sınıfı.
3. (Test için) Simülatörde veri üretimi.

DI kaydı (reflection ile otomatik keşif), parser factory, API hata mesajı (enum'dan) ve
pano gösterimi (otomatik renk/ikon/etiket) **kendiliğinden** uyum sağlar; consumer,
veritabanı ve API katmanlarına dokunulmaz. Bu, beşinci sensör (ses) eklenirken git ile
ölçülüp doğrulanmıştır.

Alternatif olarak config-driven bir yapı da değerlendirilmiş; ancak bu ölçekte bakım maliyeti,
okunabilirlik ve tip güvenliği açısından izole parser yaklaşımı tercih edilmiştir.

---

## Sayfalama Konvansiyonu

Tek ve sabit konvansiyon: **`pageIndex` (0-tabanlı) + `pageSize`**. Geçersiz değerler `400`
ile reddedilir.