using SensorNormalization.Consumer.Parsers;
using SensorNormalization.Domain.Entities;
using SensorNormalization.Domain.Messages;
using Xunit;

namespace SensorNormalization.Tests;

public class JsonTemperatureParserTests
{
    private readonly JsonTemperatureParser _parser = new();

    // Ham mesaj olusturmak icin yardimci.
    private static SensorRawReadingMessage Msg(string payload) => new()
    {
        SensorId = "TEMP-01",
        SensorType = SensorType.Temperature,
        Format = PayloadFormat.Json,
        Payload = payload,
        PublishedAtUtc = DateTime.UtcNow
    };

    [Fact]
    public void Gecerli_veri_Fahrenheit_Celsius_e_cevrilir()
    {
        // 98.6 F = 37 C (normalizasyon dogrulugu)
        var msg = Msg("{\"sensor_id\":\"TEMP-01\",\"reading_fahrenheit\":98.6,\"ts_unix\":1785393173}");

        SensorReading result = _parser.Parse(msg);

        Assert.Equal(37, result.Value);
        Assert.Equal("C", result.Unit);
        Assert.Equal(SensorType.Temperature, result.SensorType);
        Assert.Equal("TEMP-01", result.SensorId);
    }

    [Fact]
    public void Unix_zaman_UTC_ye_cevrilir()
    {
        var msg = Msg("{\"sensor_id\":\"TEMP-01\",\"reading_fahrenheit\":32,\"ts_unix\":0}");

        SensorReading result = _parser.Parse(msg);

        // ts_unix=0 => 1970-01-01T00:00:00Z
        Assert.Equal(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), result.TimestampUtc);
        Assert.Equal(0, result.Value); // 32F = 0C
    }

    [Fact]
    public void Gecersiz_sayi_NaN_hata_firlatir()
    {
        var msg = Msg("{\"sensor_id\":\"TEMP-01\",\"reading_fahrenheit\":\"NaN\",\"ts_unix\":1785393173}");

        Assert.Throws<FormatException>(() => _parser.Parse(msg));
    }

    [Fact]
    public void Eksik_alan_hata_firlatir()
    {
        var msg = Msg("{\"sensor_id\":\"TEMP-01\",\"ts_unix\":1785393173}");

        Assert.Throws<FormatException>(() => _parser.Parse(msg));
    }
}
