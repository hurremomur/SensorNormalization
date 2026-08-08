using System.Globalization;
using System.Xml.Linq;
using SensorNormalization.Domain.Entities;
using SensorNormalization.Domain.Messages;

namespace SensorNormalization.Consumer.Application.Parsers;

// Nem sensorunun XML verisini okuyup normalize eden parser.
// Ham ornek:
// <HumidityReading><DeviceId>HUM-03</DeviceId><Percentage>74.5</Percentage>
//   <Timestamp>2026-07-30T09:32:53+03:00</Timestamp></HumidityReading>
public class XmlHumidityParser : ISensorPayloadParser
{
    public PayloadFormat Format => PayloadFormat.Xml;
    public SensorType SensorType => SensorType.Humidity;

    public SensorReading Parse(SensorRawReadingMessage message)
    {
        // 1) Ham XML metnini bir nesne agacina cevir. Bozuksa XmlException firlatir.
        XDocument doc = XDocument.Parse(message.Payload);
        XElement? root = doc.Root;

        if (root is null)
            throw new FormatException("XML nem: kok eleman bulunamadi.");

        // 2) Alanlari oku (elemanlar yoksa null gelir).
        string? sensorId = (string?)root.Element("DeviceId");
        string? percentageText = (string?)root.Element("Percentage");
        string? timestampText = (string?)root.Element("Timestamp");

        // 3) Zorunlu alan kontrolu.
        if (string.IsNullOrWhiteSpace(sensorId))
            throw new FormatException("XML nem: DeviceId alani eksik.");
        if (string.IsNullOrWhiteSpace(percentageText))
            throw new FormatException("XML nem: Percentage alani eksik.");
        if (string.IsNullOrWhiteSpace(timestampText))
            throw new FormatException("XML nem: Timestamp alani eksik.");

        // 4) Yuzdeyi sayiya cevir. Nokta ondalik icin InvariantCulture kullan.
        if (!double.TryParse(percentageText, NumberStyles.Any, CultureInfo.InvariantCulture, out double percentage))
            throw new FormatException($"XML nem: Percentage sayisal degil ({percentageText}).");

        // 5) NORMALIZASYON: +03:00 gibi yerel saatli zamani UTC''ye cevir.
        //    DateTimeOffset offset bilgisini korur; .UtcDateTime ile UTC''ye ceviririz.
        if (!DateTimeOffset.TryParse(timestampText, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateTimeOffset parsedOffset))
            throw new FormatException($"XML nem: Timestamp gecersiz ({timestampText}).");

        DateTime timestampUtc = parsedOffset.UtcDateTime;

        // 6) Standart SensorReading olustur ve dondur.
        return new SensorReading
        {
            SensorId = sensorId,
            SensorType = SensorType.Humidity,
            Value = Math.Round(percentage, 2),
            Unit = "%",
            Time = timestampUtc,
            SourceFormat = PayloadFormat.Xml
        };
    }
}
