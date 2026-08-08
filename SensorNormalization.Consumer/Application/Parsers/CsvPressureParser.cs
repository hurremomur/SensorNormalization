using System.Globalization;
using SensorNormalization.Domain.Entities;
using SensorNormalization.Domain.Messages;

namespace SensorNormalization.Consumer.Application.Parsers;

// Basinc sensorunun CSV verisini okuyup normalize eden parser.
// Ham ornek (iki satir: baslik + deger):
// sensorId,value,unit,capturedAt
// PRES-02,1013.19,mbar,2026-07-30T06:32:53Z
public class CsvPressureParser : ISensorPayloadParser
{
    public PayloadFormat Format => PayloadFormat.Csv;
    public SensorType SensorType => SensorType.Pressure;

    public SensorReading Parse(SensorRawReadingMessage message)
    {
        // 1) Metni satirlara bol (bos satirlari at).
        string[] lines = message.Payload
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        // En az baslik + 1 veri satiri olmali.
        if (lines.Length < 2)
            throw new FormatException("CSV basinc: veri satiri bulunamadi.");

        // 2) Ikinci satiri (veri) kolonlara bol.
        string[] columns = lines[1].Split(',');

        // Beklenen 4 kolon: sensorId, value, unit, capturedAt
        if (columns.Length < 4)
            throw new FormatException($"CSV basinc: beklenen 4 kolon, gelen {columns.Length}.");

        string sensorId = columns[0].Trim();
        string valueText = columns[1].Trim();
        string unit = columns[2].Trim();
        string capturedAtText = columns[3].Trim();

        // 3) Zorunlu alan kontrolu.
        if (string.IsNullOrWhiteSpace(sensorId))
            throw new FormatException("CSV basinc: sensorId eksik.");
        if (string.IsNullOrWhiteSpace(valueText))
            throw new FormatException("CSV basinc: value eksik.");

        // 4) Degeri sayiya cevir (nokta ondalik icin InvariantCulture).
        if (!double.TryParse(valueText, NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
            throw new FormatException($"CSV basinc: value sayisal degil ({valueText}).");

        // 5) Zamani oku. capturedAt zaten UTC (Z) formatinda geliyor.
        if (!DateTimeOffset.TryParse(capturedAtText, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out DateTimeOffset parsedOffset))
            throw new FormatException($"CSV basinc: capturedAt gecersiz ({capturedAtText}).");

        DateTime timestampUtc = parsedOffset.UtcDateTime;

        // 6) NORMALIZASYON: birim adini standartlastir (mbar -> hPa; sayisal olarak esit).
        string normalizedUnit = unit.Equals("mbar", StringComparison.OrdinalIgnoreCase) ? "hPa" : unit;

        // 7) Standart SensorReading olustur ve dondur.
        return new SensorReading
        {
            SensorId = sensorId,
            SensorType = SensorType.Pressure,
            Value = Math.Round(value, 2),
            Unit = normalizedUnit,
            Time = timestampUtc,
            SourceFormat = PayloadFormat.Csv
        };
    }
}
