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
        new CsvPressureParser()
    });

    [Theory]
    [InlineData(PayloadFormat.Json, typeof(JsonTemperatureParser))]
    [InlineData(PayloadFormat.Xml, typeof(XmlHumidityParser))]
    [InlineData(PayloadFormat.Csv, typeof(CsvPressureParser))]
    public void Dogru_format_dogru_parseri_dondurur(PayloadFormat format, Type expected)
    {
        ISensorPayloadParser parser = _factory.GetParser(format);

        Assert.IsType(expected, parser);
    }
}
