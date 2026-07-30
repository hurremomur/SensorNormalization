using MassTransit;
using SensorNormalization.Domain.Messages;

namespace SensorNormalization.Consumer.Consumers;

public class SensorRawReadingConsumer : IConsumer<SensorRawReadingMessage>
{
    private readonly ILogger<SensorRawReadingConsumer> _logger;

    public SensorRawReadingConsumer(ILogger<SensorRawReadingConsumer> logger)
        => _logger = logger;

    public Task Consume(ConsumeContext<SensorRawReadingMessage> context)
    {
        var m = context.Message;
        _logger.LogInformation(
            "Mesaj alindi -> SensorId={SensorId}, Type={Type}, Format={Format}, Payload={Payload}",
            m.SensorId, m.SensorType, m.Format, m.Payload);
        return Task.CompletedTask;
    }
}
