using SensorNormalization.Consumer.Application.Parsers;
using SensorNormalization.Domain.Entities;
using SensorNormalization.Domain.Messages;
using Xunit;

namespace SensorNormalization.Tests;

public class XmlHumidityParserTests
{
    private readonly XmlHumidityParser _parser = new();

    private static SensorRawReadingMessage Msg(string payload) => new()
    {
        SensorId = "HUM-03",
        SensorType = SensorType.Humidity,
        Format = PayloadFormat.Xml,
        Payload = payload,
        PublishedAtUtc = DateTime.UtcNow
    };

    [Fact]
    public void Gecerli_veri_yuzde_okunur()
    {
        var msg = Msg("<HumidityReading><DeviceId>HUM-03</DeviceId><Percentage>74.5</Percentage><Timestamp>2026-07-30T09:32:53+03:00</Timestamp></HumidityReading>");

        SensorReading result = _parser.Parse(msg);

        Assert.Equal(74.5, result.Value);
        Assert.Equal("%", result.Unit);
        Assert.Equal(SensorType.Humidity, result.SensorType);
    }

    [Fact]
    public void Yerel_saat_UTC_ye_cevrilir()
    {
        // 09:32:53 +03:00  =>  06:32:53 UTC (timezone normalizasyonu)
        var msg = Msg("<HumidityReading><DeviceId>HUM-03</DeviceId><Percentage>50</Percentage><Timestamp>2026-07-30T09:32:53+03:00</Timestamp></HumidityReading>");

        SensorReading result = _parser.Parse(msg);

        Assert.Equal(new DateTime(2026, 7, 30, 6, 32, 53, DateTimeKind.Utc), result.Time);
    }

    [Fact]
    public void Eksik_Percentage_hata_firlatir()
    {
        var msg = Msg("<HumidityReading><DeviceId>HUM-03</DeviceId><Timestamp>2026-07-30T09:32:53+03:00</Timestamp></HumidityReading>");

        Assert.Throws<FormatException>(() => _parser.Parse(msg));
    }
}
