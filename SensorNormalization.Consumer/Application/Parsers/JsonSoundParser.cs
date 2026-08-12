using Newtonsoft.Json.Linq;
using SensorNormalization.Domain.Entities;
using SensorNormalization.Domain.Messages;

namespace SensorNormalization.Consumer.Application.Parsers;

// Ses/gurultu sensorunun JSON verisini okuyup normalize eden parser.
// Ham ornek: {"sensor_id":"SOUND-05","decibel":65.4,"ts_unix":1786224950}
// 5. sensor tipi -- izole parser. DI otomatik kesif ile kaydolur,
// dashboard otomatik renk/ikon ile gosterir. Baska yere dokunulmaz.
public class JsonSoundParser : ISensorPayloadParser
{
    public PayloadFormat Format => PayloadFormat.Json;
    public SensorType SensorType => SensorType.Sound;

    public SensorReading Parse(SensorRawReadingMessage message)
    {
        JObject json = JObject.Parse(message.Payload);

        string? sensorId = (string?)json["sensor_id"];
        JToken? decibelToken = json["decibel"];
        long? tsUnix = (long?)json["ts_unix"];

        if (string.IsNullOrWhiteSpace(sensorId))
            throw new FormatException("JSON ses: sensor_id alani eksik.");
        if (decibelToken is null || decibelToken.Type == JTokenType.Null)
            throw new FormatException("JSON ses: decibel alani eksik.");
        if (tsUnix is null)
            throw new FormatException("JSON ses: ts_unix alani eksik.");

        double decibel = (double)decibelToken;
        if (double.IsNaN(decibel) || double.IsInfinity(decibel))
            throw new FormatException($"JSON ses: decibel gecersiz sayi ({decibelToken}).");
        if (decibel < 0)
            throw new FormatException($"JSON ses: decibel negatif olamaz ({decibel}).");

        DateTime timestampUtc = DateTimeOffset.FromUnixTimeSeconds(tsUnix.Value).UtcDateTime;

        return new SensorReading
        {
            SensorId = sensorId,
            SensorType = SensorType.Sound,
            Value = Math.Round(decibel, 2),
            Unit = "dB",
            Time = timestampUtc,
            SourceFormat = PayloadFormat.Json
        };
    }
}
