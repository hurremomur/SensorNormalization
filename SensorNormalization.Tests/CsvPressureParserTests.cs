using SensorNormalization.Consumer.Parsers;
using SensorNormalization.Domain.Entities;
using SensorNormalization.Domain.Messages;
using Xunit;

namespace SensorNormalization.Tests;

public class CsvPressureParserTests
{
    private readonly CsvPressureParser _parser = new();

    private static SensorRawReadingMessage Msg(string payload) => new()
    {
        SensorId = "PRES-02",
        SensorType = SensorType.Pressure,
        Format = PayloadFormat.Csv,
        Payload = payload,
        PublishedAtUtc = DateTime.UtcNow
    };

    [Fact]
    public void Gecerli_veri_mbar_hPa_ya_cevrilir()
    {
        var msg = Msg("sensorId,value,unit,capturedAt\nPRES-02,1013.19,mbar,2026-07-30T06:32:53Z");

        SensorReading result = _parser.Parse(msg);

        Assert.Equal(1013.19, result.Value);
        Assert.Equal("hPa", result.Unit);        // mbar -> hPa (birim normalizasyonu)
        Assert.Equal(SensorType.Pressure, result.SensorType);
    }

    [Fact]
    public void Bos_value_hata_firlatir()
    {
        var msg = Msg("sensorId,value,unit,capturedAt\nPRES-02,,mbar,2026-07-30T06:32:53Z");

        Assert.Throws<FormatException>(() => _parser.Parse(msg));
    }
}
