using Newtonsoft.Json.Linq;
using SensorNormalization.Domain.Entities;
using SensorNormalization.Domain.Messages;

namespace SensorNormalization.Consumer.Parsers;

// Sicaklik sensorunun JSON verisini okuyup normalize eden parser.
// Ham ornek: {"sensor_id":"TEMP-01","reading_fahrenheit":84.8,"ts_unix":1785393173}
public class JsonTemperatureParser : ISensorPayloadParser
{
    public PayloadFormat Format => PayloadFormat.Json;

    public SensorReading Parse(SensorRawReadingMessage message)
    {
        // 1) Ham JSON metnini bir nesne agacina cevir. Bozuksa JsonReaderException firlatir.
        JObject json = JObject.Parse(message.Payload);

        // 2) Zorunlu alanlari oku.
        string? sensorId = (string?)json["sensor_id"];
        JToken? fahrenheitToken = json["reading_fahrenheit"];
        long? tsUnix = (long?)json["ts_unix"];

        // 3) Eksik alan kontrolu.
        if (string.IsNullOrWhiteSpace(sensorId))
            throw new FormatException("JSON sicaklik: sensor_id alani eksik.");
        if (fahrenheitToken is null || fahrenheitToken.Type == JTokenType.Null)
            throw new FormatException("JSON sicaklik: reading_fahrenheit alani eksik.");
        if (tsUnix is null)
            throw new FormatException("JSON sicaklik: ts_unix alani eksik.");

        // 4) reading_fahrenheit gercek bir sayi mi? ("NaN" gibi metinleri burada yakala.)
        double fahrenheit = (double)fahrenheitToken;
        if (double.IsNaN(fahrenheit) || double.IsInfinity(fahrenheit))
            throw new FormatException($"JSON sicaklik: reading_fahrenheit gecersiz sayi ({fahrenheitToken}).");

        // 5) NORMALIZASYON: Fahrenheit -> Celsius.
        double celsius = Math.Round((fahrenheit - 32) * 5 / 9, 2);

        // 6) NORMALIZASYON: Unix saniye -> gercek UTC tarih.
        DateTime timestampUtc = DateTimeOffset.FromUnixTimeSeconds(tsUnix.Value).UtcDateTime;

        // 7) Standart SensorReading olustur ve dondur.
        return new SensorReading
        {
            SensorId = sensorId,
            SensorType = SensorType.Temperature,
            Value = celsius,
            Unit = "C",
            TimestampUtc = timestampUtc,
            SourceFormat = PayloadFormat.Json
        };
    }
}
