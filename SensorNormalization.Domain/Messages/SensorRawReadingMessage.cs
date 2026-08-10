namespace SensorNormalization.Domain.Messages;

public class SensorRawReadingMessage
{
    public string SensorId { get; set; } = default!;
    public SensorType SensorType { get; set; }
    public PayloadFormat Format { get; set; }
    public string Payload { get; set; } = default!;
    public DateTime PublishedAtUtc { get; set; }
}

public enum SensorType { Temperature, Humidity, Pressure, Light, Sound }
public enum PayloadFormat { Json, Xml, Csv }
