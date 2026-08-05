namespace SensorNormalization.Application.Dto;

// Bir sensor tipinin belirli araliktaki istatistik ozeti (min/max/avg).
public class SensorReadingSummaryDto
{
    public string SensorType { get; set; } = default!;
    public int Count { get; set; }
    public double? Min { get; set; }
    public double? Max { get; set; }
    public double? Average { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}
