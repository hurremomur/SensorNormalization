using System.Globalization;
using MassTransit;
using SensorNormalization.Domain.Messages;

const int CorruptionRate = 5;

var bus = Bus.Factory.CreateUsingRabbitMq(cfg =>
{
    cfg.Host("localhost", "/", h =>
    {
        h.Username("guest");
        h.Password("guest");
    });
});

await bus.StartAsync();
Console.WriteLine("Simulator basladi. Durdurmak icin Ctrl+C.");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    while (!cts.IsCancellationRequested)
    {
        await PublishTemperatureAsync();
        await PublishHumidityAsync();
        await PublishPressureAsync();
        await PublishLightAsync();
        await PublishSoundAsync();

        await Task.Delay(TimeSpan.FromSeconds(5), cts.Token);
    }
}
catch (OperationCanceledException)
{
}
finally
{
    await bus.StopAsync();
    Console.WriteLine("Simulator durdu.");
}

// Bozuk (parse edilemez) veri tetikleyici.
static bool ShouldCorrupt() => Random.Shared.Next(CorruptionRate) == 0;

// Duplicate mesaj tetikleyici.
static bool ShouldDuplicate() => Random.Shared.Next(10) == 0;

// Gecerli ama asiri (uc) deger tetikleyici -> istatistiksel anomali.
static bool ShouldSpike() => Random.Shared.Next(20) == 0;

// Sicaklik -> JSON, Fahrenheit, Unix timestamp.
async Task PublishTemperatureAsync()
{
    long unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    string payload;

    if (ShouldCorrupt())
    {
        payload =
            $"{{\"sensor_id\":\"TEMP-01\"," +
            $"\"reading_fahrenheit\":\"NaN\"," +
            $"\"ts_unix\":{unixTimestamp}}}";
        Console.WriteLine("  -> hatali sicaklik uretildi (gecersiz sayi)");
    }
    else
    {
        double fahrenheit;
        if (ShouldSpike())
        {
            // Gecerli ama uc: ~63C (145F) veya ~-7C (20F). Anomali beklenir.
            fahrenheit = Random.Shared.Next(2) == 0 ? 145 : 20;
            Console.WriteLine("  -> UC sicaklik uretildi (anomali beklenir)");
        }
        else
        {
            fahrenheit = Math.Round(60 + Random.Shared.NextDouble() * 40, 1);
        }
        payload =
            $"{{\"sensor_id\":\"TEMP-01\"," +
            $"\"reading_fahrenheit\":{fahrenheit.ToString(CultureInfo.InvariantCulture)}," +
            $"\"ts_unix\":{unixTimestamp}}}";
    }

    await PublishAsync(SensorType.Temperature, PayloadFormat.Json, "TEMP-01", payload);
}

// Nem -> XML, yuzde, +03:00.
async Task PublishHumidityAsync()
{
    string timestamp = DateTimeOffset.UtcNow
        .ToOffset(TimeSpan.FromHours(3))
        .ToString("yyyy-MM-ddTHH:mm:sszzz");
    string payload;

    if (ShouldCorrupt())
    {
        payload =
            "<HumidityReading>" +
            "<DeviceId>HUM-03</DeviceId>" +
            $"<Timestamp>{timestamp}</Timestamp>" +
            "</HumidityReading>";
        Console.WriteLine("  -> hatali nem uretildi (eksik alan)");
    }
    else
    {
        double percentage;
        if (ShouldSpike())
        {
            percentage = Random.Shared.Next(2) == 0 ? 99 : 3;
            Console.WriteLine("  -> UC nem uretildi (anomali beklenir)");
        }
        else
        {
            percentage = Math.Round(30 + Random.Shared.NextDouble() * 60, 1);
        }
        payload =
            "<HumidityReading>" +
            "<DeviceId>HUM-03</DeviceId>" +
            $"<Percentage>{percentage.ToString(CultureInfo.InvariantCulture)}</Percentage>" +
            $"<Timestamp>{timestamp}</Timestamp>" +
            "</HumidityReading>";
    }

    await PublishAsync(SensorType.Humidity, PayloadFormat.Xml, "HUM-03", payload);
}

// Basinc -> CSV, mbar, UTC.
async Task PublishPressureAsync()
{
    string capturedAt = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
    string payload;

    if (ShouldCorrupt())
    {
        payload =
            "sensorId,value,unit,capturedAt\n" +
            $"PRES-02,,mbar,{capturedAt}";
        Console.WriteLine("  -> hatali basinc uretildi (bos deger)");
    }
    else
    {
        double value;
        if (ShouldSpike())
        {
            value = Random.Shared.Next(2) == 0 ? 1080 : 920;
            Console.WriteLine("  -> UC basinc uretildi (anomali beklenir)");
        }
        else
        {
            value = Math.Round(990 + Random.Shared.NextDouble() * 40, 2);
        }
        payload =
            "sensorId,value,unit,capturedAt\n" +
            $"PRES-02,{value.ToString(CultureInfo.InvariantCulture)},mbar,{capturedAt}";
    }

    await PublishAsync(SensorType.Pressure, PayloadFormat.Csv, "PRES-02", payload);
}

// Isik -> JSON, lux, Unix timestamp. (4. sensor)
async Task PublishLightAsync()
{
    long unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    string payload;

    if (ShouldCorrupt())
    {
        payload =
            $"{{\"sensor_id\":\"LIGHT-04\"," +
            $"\"lux\":\"NaN\"," +
            $"\"ts_unix\":{unixTimestamp}}}";
        Console.WriteLine("  -> hatali isik uretildi (gecersiz sayi)");
    }
    else
    {
        double lux;
        if (ShouldSpike())
        {
            lux = Random.Shared.Next(2) == 0 ? 5000 : 5;
            Console.WriteLine("  -> UC isik uretildi (anomali beklenir)");
        }
        else
        {
            lux = Math.Round(100 + Random.Shared.NextDouble() * 900, 1);
        }
        payload =
            $"{{\"sensor_id\":\"LIGHT-04\"," +
            $"\"lux\":{lux.ToString(CultureInfo.InvariantCulture)}," +
            $"\"ts_unix\":{unixTimestamp}}}";
    }

    await PublishAsync(SensorType.Light, PayloadFormat.Json, "LIGHT-04", payload);
}
// Ses/gurultu sensoru -> JSON, dB, Unix timestamp. (5. sensor tipi)
async Task PublishSoundAsync()
{
    long unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    string payload;

    if (ShouldCorrupt())
    {
        payload =
            $"{{\"sensor_id\":\"SOUND-05\"," +
            $"\"decibel\":\"NaN\"," +
            $"\"ts_unix\":{unixTimestamp}}}";
        Console.WriteLine("  -> hatali ses uretildi (gecersiz sayi)");
    }
    else
    {
        double decibel;
        if (ShouldSpike())
        {
            decibel = Random.Shared.Next(2) == 0 ? 115 : 25;
            Console.WriteLine("  -> UC ses uretildi (anomali beklenir)");
        }
        else
        {
            decibel = Math.Round(45 + Random.Shared.NextDouble() * 30, 1);
        }
        payload =
            $"{{\"sensor_id\":\"SOUND-05\"," +
            $"\"decibel\":{decibel.ToString(CultureInfo.InvariantCulture)}," +
            $"\"ts_unix\":{unixTimestamp}}}";
    }

    await PublishAsync(SensorType.Sound, PayloadFormat.Json, "SOUND-05", payload);
}
// Ham veriyi zarfa sarip yayinlar.
async Task PublishAsync(SensorType type, PayloadFormat format, string sensorId, string payload)
{
    await bus.Publish(new SensorRawReadingMessage
    {
        SensorId = sensorId,
        SensorType = type,
        Format = format,
        Payload = payload,
        PublishedAtUtc = DateTime.UtcNow
    });
    Console.WriteLine($"Yayinlandi -> {type} ({format})");

    if (ShouldDuplicate())
    {
        await bus.Publish(new SensorRawReadingMessage
        {
            SensorId = sensorId,
            SensorType = type,
            Format = format,
            Payload = payload,
            PublishedAtUtc = DateTime.UtcNow
        });
        Console.WriteLine($"  -> tekrar mesaj (duplicate) -> {type}");
    }
}