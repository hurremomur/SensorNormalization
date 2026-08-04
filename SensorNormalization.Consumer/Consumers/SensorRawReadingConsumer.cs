using MassTransit;
using SensorNormalization.Consumer.Application.Parsers;
using SensorNormalization.Application.Services.Abstract;
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

        SensorReading reading;

        // --- 1) KALICI HATA BOLGESI: parse/normalize ---
        // Bozuk veri retry ile duzelmez. Hatayi burada yakalayip loglariz ve
        // mesaji "tuketildi" sayariz (retry'a sokmayiz). Boyle mesajlar isteniyorsa
        // ayri bir gecersiz-veri kuyruguna da yonlendirilebilir.
        try
        {
            ISensorPayloadParser parser = _parserFactory.GetParser(message.Format);
            reading = parser.Parse(message);
            reading.RawPayload = message.Payload;
        }
        catch (Exception ex) when (ex is FormatException or NotSupportedException)
        {
            _logger.LogWarning(
                "Gecersiz veri (retry edilmez) -> {SensorId} | {Format} | Hata: {Error} | Ham: {Payload}",
                message.SensorId, message.Format, ex.Message, message.Payload);
            return; // mesaj tuketildi; retry yok, error queue yok (kalici bozuk).
        }

        // --- 2) GECICI HATA BOLGESI: veritabanina yaz ---
        // DB/ag gibi gecici hatalar burada firlar; Consume disari exception verir,
        // MassTransit retry uygular, tukenirse mesaj otomatik _error kuyruguna duser.
        await _service.SaveAsync(reading, context.CancellationToken);

        _logger.LogInformation(
            "Kaydedildi -> {SensorId} | {Type} | {Value} {Unit} | {Time:o} | kaynak={Source}",
            reading.SensorId, reading.SensorType, reading.Value, reading.Unit,
            reading.Time, reading.SourceFormat);
    }
}
