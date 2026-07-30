using System.Globalization;
using MassTransit;
using SensorNormalization.Domain.Messages;

// Her N mesajdan yaklaşık 1'i bilinçli olarak bozulur; böylece sonraki
// fazlardaki hata yönetimi (parse/validation) mantığı test edilebilir.
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
Console.WriteLine("Simülatör başladı. Durdurmak için Ctrl+C.");

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

        await Task.Delay(TimeSpan.FromSeconds(3), cts.Token);
    }
}
catch (OperationCanceledException)
{
    // Ctrl+C ile normal (beklenen) çıkış.
}
finally
{
    await bus.StopAsync();
    Console.WriteLine("Simülatör durdu.");
}

// Mesajların küçük, rastgele bir kısmı için true döner.
static bool ShouldCorrupt() => Random.Shared.Next(CorruptionRate) == 0;

// Sıcaklık sensörü → JSON, Fahrenheit, Unix timestamp.
async Task PublishTemperatureAsync()
{
    long unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    string payload;

    if (ShouldCorrupt())
    {
        // Bozuk: sayısal alan sayı yerine metin içeriyor.
        payload =
            $"{{\"sensor_id\":\"TEMP-01\"," +
            $"\"reading_fahrenheit\":\"NaN\"," +
            $"\"ts_unix\":{unixTimestamp}}}";
        Console.WriteLine("  -> hatalı sıcaklık üretildi (geçersiz sayı)");
    }
    else
    {
        double fahrenheit = Math.Round(60 + Random.Shared.NextDouble() * 40, 1);
        payload =
            $"{{\"sensor_id\":\"TEMP-01\"," +
            $"\"reading_fahrenheit\":{fahrenheit.ToString(CultureInfo.InvariantCulture)}," +
            $"\"ts_unix\":{unixTimestamp}}}";
    }

    await PublishAsync(SensorType.Temperature, PayloadFormat.Json, "TEMP-01", payload);
}

// Nem sensörü → XML, yüzde, yerel saat (+03:00).
async Task PublishHumidityAsync()
{
    string timestamp = DateTimeOffset.UtcNow
        .ToOffset(TimeSpan.FromHours(3))
        .ToString("yyyy-MM-ddTHH:mm:sszzz");
    string payload;

    if (ShouldCorrupt())
    {
        // Bozuk: Percentage alanı tamamen eksik.
        payload =
            "<HumidityReading>" +
            "<DeviceId>HUM-03</DeviceId>" +
            $"<Timestamp>{timestamp}</Timestamp>" +
            "</HumidityReading>";
        Console.WriteLine("  -> hatalı nem üretildi (eksik alan)");
    }
    else
    {
        double percentage = Math.Round(30 + Random.Shared.NextDouble() * 60, 1);
        payload =
            "<HumidityReading>" +
            "<DeviceId>HUM-03</DeviceId>" +
            $"<Percentage>{percentage.ToString(CultureInfo.InvariantCulture)}</Percentage>" +
            $"<Timestamp>{timestamp}</Timestamp>" +
            "</HumidityReading>";
    }

    await PublishAsync(SensorType.Humidity, PayloadFormat.Xml, "HUM-03", payload);
}

// Basınç sensörü → CSV, mbar, UTC.
async Task PublishPressureAsync()
{
    string capturedAt = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
    string payload;

    if (ShouldCorrupt())
    {
        // Bozuk: value kolonu boş.
        payload =
            "sensorId,value,unit,capturedAt\n" +
            $"PRES-02,,mbar,{capturedAt}";
        Console.WriteLine("  -> hatalı basınç üretildi (boş değer)");
    }
    else
    {
        double value = Math.Round(990 + Random.Shared.NextDouble() * 40, 2);
        payload =
            "sensorId,value,unit,capturedAt\n" +
            $"PRES-02,{value.ToString(CultureInfo.InvariantCulture)},mbar,{capturedAt}";
    }

    await PublishAsync(SensorType.Pressure, PayloadFormat.Csv, "PRES-02", payload);
}

// Ham veriyi taşıma zarfına (envelope) sarıp yayınlar.
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

    Console.WriteLine($"Yayınlandı -> {type} ({format})");
}