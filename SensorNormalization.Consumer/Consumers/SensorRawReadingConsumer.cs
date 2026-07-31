using MassTransit;
using SensorNormalization.Consumer.Parsers;
using SensorNormalization.Domain.Entities;
using SensorNormalization.Domain.Messages;

namespace SensorNormalization.Consumer.Consumers;

public class SensorRawReadingConsumer : IConsumer<SensorRawReadingMessage>
{
    private readonly ILogger<SensorRawReadingConsumer> _logger;
    private readonly SensorPayloadParserFactory _parserFactory;

    // Logger ve parser fabrikasi disaridan (DI ile) enjekte edilir.
    public SensorRawReadingConsumer(
        ILogger<SensorRawReadingConsumer> logger,
        SensorPayloadParserFactory parserFactory)
    {
        _logger = logger;
        _parserFactory = parserFactory;
    }

    public Task Consume(ConsumeContext<SensorRawReadingMessage> context)
    {
        var message = context.Message;

        try
        {
            // 1) Formata gore dogru parseri sec (factory).
            ISensorPayloadParser parser = _parserFactory.GetParser(message.Format);

            // 2) Ham veriyi normalize edilmis SensorReading nesnesine cevir.
            SensorReading reading = parser.Parse(message);

            // 3) Normalize sonucu logla.
            _logger.LogInformation(
                "Normalize edildi -> {SensorId} | {Type} | {Value} {Unit} | {TimestampUtc:o} | kaynak={Source}",
                reading.SensorId, reading.SensorType, reading.Value, reading.Unit,
                reading.TimestampUtc, reading.SourceFormat);
        }
        catch (Exception ex)
        {
            // 4) Bozuk/gecersiz veri: sistemi durdurma, hatayi logla ve devam et.
            _logger.LogWarning(
                "Mesaj islenemedi (atlandi) -> {SensorId} | {Format} | Hata: {Error} | Ham: {Payload}",
                message.SensorId, message.Format, ex.Message, message.Payload);
        }

        return Task.CompletedTask;
    }
}
