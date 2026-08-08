using SensorNormalization.Domain.Messages;

namespace SensorNormalization.Domain.Entities;

public class SensorReading
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Time { get; set; }
    public string SensorId { get; set; } = default!;
    public SensorType SensorType { get; set; }
    public double Value { get; set; }
    public string Unit { get; set; } = default!;
    public PayloadFormat SourceFormat { get; set; }
    public string? RawPayload { get; set; }
    public DateTime ReceivedAtUtc { get; set; }

    // Deger normal aralik disinda mi? (esik tabanli anomali tespiti)
    public bool IsAnomaly { get; set; }
}
