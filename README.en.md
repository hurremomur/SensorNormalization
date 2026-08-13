# Sensor Data Normalization

An end-to-end data pipeline that **normalizes** raw sensor data arriving in different
formats (JSON, XML, CSV) and units into a single canonical model, **stores** it in a
time-series database, **exposes** it through a REST reporting API, and makes it
**observable** on a live dashboard. It is a compact, educational replica of the company's
real `Platform360.MES.TSDBConsumer` service.

Data flow: **Simulator → RabbitMQ → Consumer (parse + normalize) → TimescaleDB → Reporting API → Vue Dashboard**

---

## Normalization Rules

The system supports five sensor types. Each arrives in a different format, unit, and time
representation; all are reduced to a single `SensorReading` model on a UTC time axis.

| Sensor | Raw format | Raw unit/time | Normalized |
|---|---|---|---|
| Temperature | JSON | Fahrenheit, Unix time | Celsius (`°C`), UTC |
| Humidity | XML | Percent, +03:00 local time | Percent (`%`), UTC |
| Pressure | CSV | mbar, UTC | hPa, UTC |
| Light | JSON | lux, Unix time | lux, UTC |
| Sound | JSON | dB, Unix time | dB, UTC |

Normalization works across three dimensions at once: **format** (JSON/XML/CSV → one model),
**unit** (e.g. `(F-32)*5/9`), **time** (Unix and local time → UTC).

---

## Highlights

- **Three-axis normalization** — format, unit, and time are reduced to one standard together.
- **Automatic format detection** — even without a format hint, it is inferred from the
  content (`{`, `<`, delimiter).
- **Statistical anomaly detection** — instead of a fixed threshold, mean ± 3σ is computed
  from each sensor's recent readings; the system learns each sensor's normal from the data.
- **Fault tolerance** — malformed data is rejected, transient errors are retried, and
  permanent failures are moved to a dead-letter queue.
- **Extensibility** — parsers are auto-discovered via reflection; when a new sensor is added,
  DI, the API error message, and the UI adapt on their own (Open/Closed principle).
- **Live monitoring dashboard** — a Platform360-style, self-refreshing UI built with
  Vue + Vuetify.

---

## Architecture

```
SensorNormalization.Domain/         Shared model (entity, message, enum)
SensorNormalization.Application/    Shared layer: DbContext + Service + Repository + Migrations
SensorNormalization.Consumer/       Write side: consumes from RabbitMQ, normalizes, writes to DB
SensorNormalization.Api/            Read side: reporting REST API (Swagger)
SensorNormalization.Simulator/      Fake sensor data producer
SensorNormalization.Tests/          Unit + integration tests
frontend/sensor-dashboard/          Vue 2 + Vuetify 2 live monitoring dashboard
```

`Consumer` (write) and `Api` (read) are separate applications that share the common
`Application` layer, allowing them to scale independently.

### Design patterns

- **Strategy** — a separate `ISensorPayloadParser` per format/type.
- **Factory** — selects the correct parser by the incoming message's format + type pair.
- **Repository + Dependency Injection** — abstracts data access and eases testing.

---

## Tech Stack

- **.NET 8** (C#)
- **Vue 2 + Vuetify 2** — live monitoring dashboard
- **MassTransit 8.3** + **RabbitMQ** — messaging
- **TimescaleDB** (PostgreSQL) — time-series database (hypertable)
- **Entity Framework Core 8** + **Npgsql** — ORM
- **Docker Compose** — RabbitMQ, TimescaleDB, pgAdmin
- **xUnit** — unit + integration tests
- **Swagger / OpenAPI** — API documentation

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Node.js](https://nodejs.org) (only to run the dashboard)

---

## Setup & Run

### 1. Start infrastructure (Docker)

```bash
docker compose up -d
```

This starts three containers:
- RabbitMQ (management UI: http://localhost:15672 — `guest`/`guest`)
- TimescaleDB (port 5432)
- pgAdmin (http://localhost:5050 — `admin@admin.com`/`admin`)

### 2. Apply the database schema (first-time setup only)

```bash
dotnet ef database update --project SensorNormalization.Application --startup-project SensorNormalization.Consumer
```

Convert the table to a hypertable (first-time only):

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

Within a few seconds the Consumer logs saved records and data is written to TimescaleDB.

### 5. Start the Reporting API — separate terminal

```bash
dotnet run --project SensorNormalization.Api
```

Swagger: **http://localhost:5160/swagger**

### 6. Start the monitoring dashboard — separate terminal

```bash
cd frontend/sensor-dashboard
npm install
npm run serve
```

Dashboard: **http://localhost:8080** — five sensors stream live, with raw/normalized
details and anomaly indicators.

---

## API Endpoints

Base path: `http://localhost:5160/api/sensor-readings`
`{sensorType}` values: `temperature`, `humidity`, `pressure`, `light`, `sound`

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
```

Response: `{ "pageIndex", "pageSize", "totalCount", "items": [...] }`

### Statistical summary (min / max / average)

```bash
curl http://localhost:5160/api/sensor-readings/temperature/summary
```

Response: `{ "sensorType", "count", "min", "max", "average", "fromUtc", "toUtc" }`

### Error model

- `400 Bad Request` — invalid `sensorType`, `from > to`, invalid paging value.
  For an invalid type, the list of expected values is generated automatically from the enum.
- `404 Not Found` — valid type but no records.

---

## Tests

```bash
dotnet test
```

Coverage: per-parser normalization correctness (F→C, +03:00→UTC, mbar→hPa) and
malformed-data scenarios; format detection; parser factory selection; statistical anomaly
(mean ± 3σ) unit tests; and integration tests that connect to a real TimescaleDB.

---

## Fault Tolerance

- **Malformed data (permanent error):** the parser throws a meaningful error for an
  unparseable message.
- **Transient error (DB/network):** retried with increasing intervals (1, 2, 5 s) via
  MassTransit `UseMessageRetry`.
- **Permanent failure:** once retries are exhausted, MassTransit automatically moves the
  message to the `sensor-readings-queue_error` (dead-letter) queue; no data is lost.

Note: retry is configured explicitly; the dead-letter queue is provided automatically by
MassTransit once retry is defined.

---

## Extensibility

Adding a new sensor type touches only a few files by design:

1. A value in the `SensorType` enum.
2. A parser class implementing `ISensorPayloadParser`.
3. (For testing) data generation in the simulator.

DI registration (auto-discovered via reflection), the parser factory, the API error message
(from the enum), and the dashboard display (automatic color/icon/label) all adapt on their
own; the consumer, database, and API layers are untouched. This was measured and verified
with git when the fifth sensor (sound) was added.

A config-driven approach was also evaluated; however, at this scale, the isolated-parser
approach was preferred for maintainability, readability, and type safety.

---

## Pagination Convention

A single, fixed convention: **`pageIndex` (0-based) + `pageSize`**. Invalid values are
rejected with `400`.