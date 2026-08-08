using Newtonsoft.Json.Linq;
using SensorNormalization.Domain.Entities;
using SensorNormalization.Domain.Messages;

namespace SensorNormalization.Consumer.Application.Parsers;

// Isik sensorunun JSON verisini okuyup normalize eden parser.
// Ham ornek: {"sensor_id":"LIGHT-04","lux":350.5,"ts_unix":1785393173}
// 4. sensor tipi - sadece bu dosya + enum degeri + DI kaydi eklendi;
// consumer/factory/DB/API hic degismedi (Open/Closed ilkesi).
public class JsonLightParser : ISensorPayloadParser
{
    public PayloadFormat Format => PayloadFormat.Json;
    public SensorType SensorType => SensorType.Light;

    public SensorReading Parse(SensorRawReadingMessage message)
    {
        JObject json = JObject.Parse(message.Payload);

        string? sensorId = (string?)json["sensor_id"];
        JToken? luxToken = json["lux"];
        long? tsUnix = (long?)json["ts_unix"];

        if (string.IsNullOrWhiteSpace(sensorId))
            throw new FormatException("JSON isik: sensor_id alani eksik.");
        if (luxToken is null || luxToken.Type == JTokenType.Null)
            throw new FormatException("JSON isik: lux alani eksik.");
        if (tsUnix is null)
            throw new FormatException("JSON isik: ts_unix alani eksik.");

        double lux = (double)luxToken;
        if (double.IsNaN(lux) || double.IsInfinity(lux))
            throw new FormatException($"JSON isik: lux gecersiz sayi ({luxToken}).");
        if (lux < 0)
            throw new FormatException($"JSON isik: lux negatif olamaz ({lux}).");

        // lux standart birim; sadece UTC zaman donusumu yapilir.
        DateTime timestampUtc = DateTimeOffset.FromUnixTimeSeconds(tsUnix.Value).UtcDateTime;

        return new SensorReading
        {
            SensorId = sensorId,
            SensorType = SensorType.Light,
            Value = Math.Round(lux, 2),
            Unit = "lux",
            Time = timestampUtc,
            SourceFormat = PayloadFormat.Json
        };
    }
}
