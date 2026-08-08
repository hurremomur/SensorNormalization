using SensorNormalization.Domain.Messages;

namespace SensorNormalization.Consumer.Application.Parsers;

// Gelen mesajin (format, sensorType) ikilisine gore dogru parseri secen fabrika (Strategy secici).
public class SensorPayloadParserFactory
{
    // Anahtar artik sadece Format degil, (Format, SensorType) ikilisi:
    // ayni formatta (orn. Json) birden fazla sensor tipi (Temperature, Light) cakismadan durabiliyor.
    private readonly Dictionary<(PayloadFormat Format, SensorType SensorType), ISensorPayloadParser> _parsers;

    public SensorPayloadParserFactory(IEnumerable<ISensorPayloadParser> parsers)
    {
        _parsers = parsers.ToDictionary(p => (p.Format, p.SensorType));
    }

    // Belirli bir (format, sensorType) icin parser dondurur.
    public ISensorPayloadParser GetParser(PayloadFormat format, SensorType sensorType)
    {
        if (_parsers.TryGetValue((format, sensorType), out ISensorPayloadParser? parser))
            return parser;
        throw new NotSupportedException(
            $"Bu format/sensor tipi icin parser bulunamadi: {format} / {sensorType}");
    }

    // Mesaja gore parser secer.
    // Once mesajdaki Format+SensorType alanlari kayitli bir parser'a denk geliyorsa onu kullanir;
    // denk gelmiyorsa (etiket yok/yanlis) icerikten formati tespit edip SensorType ile birlikte dener.
    public ISensorPayloadParser GetParserFor(SensorRawReadingMessage message)
    {
        // 1) Etikete guven: (Format, SensorType) kayitli bir parser'a denk geliyorsa onu kullan.
        if (_parsers.TryGetValue((message.Format, message.SensorType), out ISensorPayloadParser? byFormatAndType))
            return byFormatAndType;

        // 2) Fallback: icerikten formati tespit et, SensorType'i mesajdan al.
        PayloadFormat detected = ContentFormatDetector.Detect(message.Payload);
        return GetParser(detected, message.SensorType);
    }
}