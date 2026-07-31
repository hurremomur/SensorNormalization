using MassTransit;
using SensorNormalization.Consumer.Consumers;
using SensorNormalization.Consumer.Parsers;

var builder = Host.CreateApplicationBuilder(args);

// --- Parser kayitlari (Strategy deseni) ---
// Her parser ISensorPayloadParser olarak kaydedilir; DI hepsini bir liste
// halinde factory''ye enjekte eder. Yeni format = yeni satir, gerisi degismez.
builder.Services.AddSingleton<ISensorPayloadParser, JsonTemperatureParser>();
builder.Services.AddSingleton<ISensorPayloadParser, XmlHumidityParser>();
builder.Services.AddSingleton<ISensorPayloadParser, CsvPressureParser>();
builder.Services.AddSingleton<SensorPayloadParserFactory>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<SensorRawReadingConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ReceiveEndpoint("sensor-readings-queue", e =>
        {
            e.ConfigureConsumer<SensorRawReadingConsumer>(context);
        });
    });
});

builder.Build().Run();
