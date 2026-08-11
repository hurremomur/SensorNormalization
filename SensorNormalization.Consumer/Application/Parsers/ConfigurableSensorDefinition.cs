namespace SensorNormalization.Consumer.Application.Parsers;

// Config'den okunan basit sensor tanimi.
// "Bir alani oku, birim etiketi koy, Unix zamani UTC'ye cevir" turu basit
// sensorler icindir; karmasik donusumler (F->C, XML) kodlu parser'da kalir.
public class ConfigurableSensorDefinition
{
    public string SensorType { get; set; } = default!;
    public string SensorIdField { get; set; } = default!;
    public string ValueField { get; set; } = default!;
    public string TimestampField { get; set; } = default!;
    public string Unit { get; set; } = default!;
}

public class ConfigurableSensorOptions
{
    public List<ConfigurableSensorDefinition> ConfigurableSensors { get; set; } = new();
}
