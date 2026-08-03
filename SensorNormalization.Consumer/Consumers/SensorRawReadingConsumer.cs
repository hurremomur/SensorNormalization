using MassTransit;
using SensorNormalization.Consumer.Application.Parsers;
using SensorNormalization.Consumer.Application.Services.Abstract;
using SensorNormalization.Domain.Entities;
using SensorNormalization.Domain.Messages;

namespace SensorNormalization.Consumer.Consumers;

public class SensorRawReadingConsumer : IConsumer<SensorRawReadingMessage>
{
    private readonly ILogger<SensorRawReadingConsumer> _logger;
    private readonly SensorPayloadParserFactory _parserFactory;
    private readonly ISensorReadingService _service;

    public SensorRawReadingConsumer(
        ILogger<SensorRawReadingConsumer> logger,
        SensorPayloadParserFactory parserFactory,
        ISensorReadingService service)
    {
        _logger = logger;
        _parserFactory = parserFactory;
        _service = service;
    }

    public async Task Consume(ConsumeContext<SensorRawReadingMessage> context)
    {
        var message = context.Message;

        try
        {
            // 1) Formata gore parseri sec ve normalize et.
            ISensorPayloadParser parser = _parserFactory.GetParser(message.Format);
            SensorReading reading = parser.Parse(message);

            // 2) Ham veriyi sakla (denetim/yeniden isleme icin).
            reading.RawPayload = message.Payload;

            // 3) Is katmanina devret (servis repository''e yazar).
            await _service.SaveAsync(reading, context.CancellationToken);

            // 4) Basarili sonucu logla.
            _logger.LogInformation(
                "Kaydedildi -> {SensorId} | {Type} | {Value} {Unit} | {Time:o} | kaynak={Source}",
                reading.SensorId, reading.SensorType, reading.Value, reading.Unit,
                reading.Time, reading.SourceFormat);
        }
        catch (Exception ex)
        {
            // 5) Bozuk/gecersiz veri: sistemi durdurma, uyari logla ve devam et.
            _logger.LogWarning(
                "Mesaj islenemedi (atlandi) -> {SensorId} | {Format} | Hata: {Error} | Ham: {Payload}",
                message.SensorId, message.Format, ex.Message, message.Payload);
        }
    }
}
