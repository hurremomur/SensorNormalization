# Sensor Data Normalization

An end-to-end data pipeline that **normalizes** raw sensor data arriving in different
formats (JSON, XML, CSV) and units into a single canonical model, **stores** it in a
time-series database, and **exposes** it through a REST reporting API. It is a compact,
educational replica of the company's real `Platform360.MES.TSDBConsumer` service.

Data flow: **Simulator → RabbitMQ → Consumer (parse + normalize) → TimescaleDB → Reporting API**

---

## Normalization Rules

| Sensor | Raw format | Raw unit/time | Normalized |
|---|---|---|---|
| Temperature | JSON | Fahrenheit, Unix time | Celsius (`C`), UTC |
| Humidity | XML | Percent, +03:00 local time | Percent (`%`), UTC |
| Pressure | CSV | mbar, UTC | hPa, UTC |

All readings are reduced to a single `SensorReading` model on a UTC time axis.

---

## Architecture

```
SensorNormalization.Domain/        Shared model (entity, message, enum)
SensorNormalization.Application/    Shared layer: DbContext + Service + Repository + Migrations
SensorNormalization.Consumer/       Write side: consumes from RabbitMQ, normalizes, writes to DB
SensorNormalization.Api/            Read side: reporting REST API (Swagger)
SensorNormalization.Simulator/      Fake sensor data producer
SensorNormalization.Tests/          Unit tests (parsers)
```

`Consumer` (write) and `Api` (read) are separate applications that share the common
`Application` layer, allowing them to scale independently. See the Architecture Decision
Record (ADR) under `docs/` for the detailed rationale.

---

## Tech Stack

- **.NET 8** (C#)
- **MassTransit 8.3** + **RabbitMQ** — messaging
- **TimescaleDB** (PostgreSQL 16) — time-series database (hypertable)
- **Entity Framework Core 8** + **Npgsql** — ORM
- **Docker Compose** — RabbitMQ, TimescaleDB, pgAdmin
- **xUnit** — unit tests
- **Swagger / OpenAPI** — API documentation

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)

---

## Setup & Run

### 1. Start infrastructure (Docker)

```bash
docker compose up -d
```

This starts three containers:
- RabbitMQ (management UI: http://localhost:15672 — user/pass: `guest`/`guest`)
- TimescaleDB (port 5432)
- pgAdmin (http://localhost:5050 — `admin@admin.com`/`admin`)

### 2. Apply the database schema (first-time setup only)

```bash
dotnet ef database update --project SensorNormalization.Application --startup-project SensorNormalization.Consumer
```

This creates the `sensor_readings` table. Convert it to a hypertable (first-time only):

```bash
docker exec sensor-timescaledb psql -U postgres -d sensordb -c "SELECT create_hypertable('sensor_readings', 'Time');"
```

### 3. Start the Consumer (write side)

```bash
dotnet run --project SensorNormalization.Consumer
```

### 4. Start the Simulator (data generation) — separate terminal

```bash
dotnet run --project SensorNormalization.Simulator
```

Within a few seconds the Consumer logs `Kaydedildi -> ...` (Saved) lines and data is
written to TimescaleDB.

### 5. Start the Reporting API — separate terminal

```bash
dotnet run --project SensorNormalization.Api
```

Swagger opens at: **http://localhost:5160/swagger**

---

## API Endpoints

Base path: `http://localhost:5160/api/sensor-readings`
`{sensorType}` values: `temperature`, `humidity`, `pressure`

### Latest value per type

```bash
curl http://localhost:5160/api/sensor-readings/latest
```

### Latest value of a specific type

```bash
curl http://localhost:5160/api/sensor-readings/temperature/latest
```

### Paged history (date range optional)

```bash
curl "http://localhost:5160/api/sensor-readings/temperature/history?pageIndex=0&pageSize=10"
curl "http://localhost:5160/api/sensor-readings/pressure/history?from=2026-08-01T00:00:00Z&to=2026-08-05T00:00:00Z&pageIndex=0&pageSize=20"
```

Response: `{ "pageIndex", "pageSize", "totalCount", "items": [...] }`

### Statistical summary (min / max / average)

```bash
curl http://localhost:5160/api/sensor-readings/temperature/summary
curl "http://localhost:5160/api/sensor-readings/humidity/summary?from=2026-08-01T00:00:00Z&to=2026-08-05T00:00:00Z"
```

Response: `{ "sensorType", "count", "min", "max", "average", "fromUtc", "toUtc" }`

### Error model

- `400 Bad Request` — invalid `sensorType`, `from > to`, invalid paging value
- `404 Not Found` — valid type but no records

---

## Tests

```bash
dotnet test
```

Runs unit tests for the parsers (normalization correctness + malformed-data scenarios).

---

## Fault Tolerance

- **Malformed data (permanent error):** an unparseable message is logged and skipped; the
  system keeps running.
- **Transient error (DB/network):** retried with increasing intervals via MassTransit
  `UseMessageRetry`.
- **Permanent failure:** once retries are exhausted, the message is moved to the
  `sensor-readings-queue_error` (dead-letter) queue; no data is lost and it can be
  inspected later.

---

## Pagination Convention

A single, fixed convention is used across the system: **`pageIndex` (0-based) + `pageSize`**.
It is never mixed with `offset/limit`. Invalid values are rejected with `400`.
