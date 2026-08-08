namespace SensorNormalization.Application.Dto;

public class SensorReadingDto
{
    public string SensorId { get; set; } = default!;
    public string SensorType { get; set; } = default!;
    public double Value { get; set; }
    public string Unit { get; set; } = default!;
    public DateTime Time { get; set; }
    public string SourceFormat { get; set; } = default!;
    public string? RawPayload { get; set; }
    public bool IsAnomaly { get; set; }
}
