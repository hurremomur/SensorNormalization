using Newtonsoft.Json.Linq;
using SensorNormalization.Domain.Entities;
using SensorNormalization.Domain.Messages;

namespace SensorNormalization.Consumer.Application.Parsers;

// Config'den gelen bir tanima gore calisan, kod yazilmadan uretilebilen parser.
// Ayni sinif, farkli tanimlarla farkli sensorlere hizmet eder. Yeni bir basit
// sensor eklemek icin bu sinifi degil, yalnizca config dosyasini degistirmek yeterlidir.
public class ConfigurableJsonParser : ISensorPayloadParser
{
    private readonly ConfigurableSensorDefinition _def;
    private readonly SensorType _sensorType;

    public ConfigurableJsonParser(ConfigurableSensorDefinition def)
    {
        _def = def;
        _sensorType = Enum.Parse<SensorType>(def.SensorType);
    }

    public PayloadFormat Format => PayloadFormat.Json;
    public SensorType SensorType => _sensorType;

    public SensorReading Parse(SensorRawReadingMessage message)
    {
        JObject json = JObject.Parse(message.Payload);

        string? sensorId = (string?)json[_def.SensorIdField];
        JToken? valueToken = json[_def.ValueField];
        long? tsUnix = (long?)json[_def.TimestampField];

        if (string.IsNullOrWhiteSpace(sensorId))
            throw new FormatException($"{_def.SensorType}: {_def.SensorIdField} alani eksik.");
        if (valueToken is null || valueToken.Type == JTokenType.Null)
            throw new FormatException($"{_def.SensorType}: {_def.ValueField} alani eksik.");
        if (tsUnix is null)
            throw new FormatException($"{_def.SensorType}: {_def.TimestampField} alani eksik.");

        double value = (double)valueToken;
        if (double.IsNaN(value) || double.IsInfinity(value))
            throw new FormatException($"{_def.SensorType}: {_def.ValueField} gecersiz sayi ({valueToken}).");

        DateTime timestampUtc = DateTimeOffset.FromUnixTimeSeconds(tsUnix.Value).UtcDateTime;

        return new SensorReading
        {
            SensorId = sensorId,
            SensorType = _sensorType,
            Value = Math.Round(value, 2),
            Unit = _def.Unit,
            Time = timestampUtc,
            SourceFormat = PayloadFormat.Json
        };
    }
}
