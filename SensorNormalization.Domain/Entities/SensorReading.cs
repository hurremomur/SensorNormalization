using SensorNormalization.Domain.Messages;
namespace SensorNormalization.Domain.Entities;

// Üç ham formatın (JSON/XML/CSV) da normalize edilip dönüştürüldüğü ortak model.
// Sistemin geri kalanı artık ham formatları değil, yalnızca bu standart yapıyı tanır.
public class SensorReading
{
    // Sensörün kimliği (ör. "TEMP-01").
    public string SensorId { get; set; } = default!;

    // Sensör tipi (Temperature / Humidity / Pressure).
    public SensorType SensorType { get; set; }

    // Normalize edilmiş ölçüm değeri (ör. sıcaklık Celsius, basınç hPa).
    public double Value { get; set; }

    // Standart birim (ör. "°C", "%", "hPa").
    public string Unit { get; set; } = default!;

    // Ölçümün alındığı zaman — her zaman UTC olarak tutulur.
    public DateTime TimestampUtc { get; set; }

    // Verinin hangi ham formattan geldiği (denetim/izlenebilirlik için).
    public PayloadFormat SourceFormat { get; set; }
}