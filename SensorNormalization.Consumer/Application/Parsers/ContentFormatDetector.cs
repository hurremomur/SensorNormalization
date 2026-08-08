using System.Text.Json;
using SensorNormalization.Domain.Messages;

namespace SensorNormalization.Consumer.Application.Parsers;

// Ham veri icerigine bakarak formatini tespit eder.
// "Format alani gelmezse icerikten anla" senaryosu icin (odev 4 bonus).
public static class ContentFormatDetector
{
    // Icerigi inceleyip PayloadFormat dondurur. Tespit edilemezse hata firlatir.
    public static PayloadFormat Detect(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            throw new NotSupportedException("Bos icerik: format tespit edilemedi.");

        string trimmed = payload.TrimStart();

        // 1) JSON: '{' ile baslar VE gercekten parse edilebiliyorsa.
        if (trimmed.StartsWith("{") || trimmed.StartsWith("["))
        {
            if (IsValidJson(trimmed))
                return PayloadFormat.Json;
        }

        // 2) XML: '<' ile baslar.
        if (trimmed.StartsWith("<"))
            return PayloadFormat.Xml;

        // 3) CSV: virgul iceren, basligi olan duz metin.
        //    (JSON/XML degilse ve virgul varsa CSV kabul edilir.)
        if (trimmed.Contains(","))
            return PayloadFormat.Csv;

        throw new NotSupportedException("Icerikten format tespit edilemedi.");
    }

    private static bool IsValidJson(string text)
    {
        try
        {
            using var _ = JsonDocument.Parse(text);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
