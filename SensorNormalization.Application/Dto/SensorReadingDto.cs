namespace SensorNormalization.Application.Dto;

// Tek bir normalize okuma (API disari boyle verir - entity''nin sade hali).
public class SensorReadingDto
{
    public string SensorId { get; set; } = default!;
    public string SensorType { get; set; } = default!;
    public double Value { get; set; }
    public string Unit { get; set; } = default!;
    public DateTime Time { get; set; }
    public string SourceFormat { get; set; } = default!;
}
