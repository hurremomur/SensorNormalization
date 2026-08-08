using SensorNormalization.Consumer.Application.Parsers;
using SensorNormalization.Domain.Messages;
using Xunit;
namespace SensorNormalization.Tests;
public class SensorPayloadParserFactoryTests
{
    private readonly SensorPayloadParserFactory _factory = new(new ISensorPayloadParser[]
    {
        new JsonTemperatureParser(),
        new XmlHumidityParser(),
        new CsvPressureParser(),
        new JsonLightParser()
    });
    [Theory]
    [InlineData(PayloadFormat.Json, SensorType.Temperature, typeof(JsonTemperatureParser))]
    [InlineData(PayloadFormat.Xml, SensorType.Humidity, typeof(XmlHumidityParser))]
    [InlineData(PayloadFormat.Csv, SensorType.Pressure, typeof(CsvPressureParser))]
    [InlineData(PayloadFormat.Json, SensorType.Light, typeof(JsonLightParser))]
    public void Dogru_format_ve_tip_dogru_parseri_dondurur(PayloadFormat format, SensorType sensorType, Type expected)
    {
        ISensorPayloadParser parser = _factory.GetParser(format, sensorType);
        Assert.IsType(expected, parser);
    }
}