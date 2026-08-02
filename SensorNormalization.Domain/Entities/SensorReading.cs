using SensorNormalization.Domain.Messages;

namespace SensorNormalization.Domain.Entities;

// Uc ham formatin (JSON/XML/CSV) da normalize edilip donusturuldugu ortak model.
// Ayni zamanda TimescaleDB hypertable satiri (odev 5.4).
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
}
