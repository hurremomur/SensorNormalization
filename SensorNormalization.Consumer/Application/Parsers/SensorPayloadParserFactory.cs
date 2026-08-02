using SensorNormalization.Domain.Messages;

namespace SensorNormalization.Consumer.Application.Parsers;

// Gelen mesajin formatina gore dogru parseri secen fabrika (Strategy secici).
// Tum parserlar disaridan (dependency injection ile) verilir; fabrika
// bunlari formatlarina gore bir sozluge yerlestirir ve talep edileni dondurur.
public class SensorPayloadParserFactory
{
    private readonly Dictionary<PayloadFormat, ISensorPayloadParser> _parsers;

    // DI, tum ISensorPayloadParser uygulamalarini otomatik olarak buraya enjekte eder.
    public SensorPayloadParserFactory(IEnumerable<ISensorPayloadParser> parsers)
    {
        // Her parseri kendi Format degerine gore sozluge ekle.
        _parsers = parsers.ToDictionary(p => p.Format);
    }

    // Verilen format icin uygun parseri dondurur.
    // Kayitli bir parser yoksa anlamli bir hata firlatir.
    public ISensorPayloadParser GetParser(PayloadFormat format)
    {
        if (_parsers.TryGetValue(format, out ISensorPayloadParser? parser))
            return parser;

        throw new NotSupportedException($"Bu format icin parser bulunamadi: {format}");
    }
}
