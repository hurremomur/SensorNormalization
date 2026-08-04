using MassTransit;
using Microsoft.EntityFrameworkCore;
using SensorNormalization.Consumer.Application.Parsers;
using SensorNormalization.Application.Repositories;
using SensorNormalization.Application.Services.Abstract;
using SensorNormalization.Application.Services.Concrete;
using SensorNormalization.Consumer.Consumers;
using SensorNormalization.Application.Infrastructure.Contexts;

var builder = Host.CreateApplicationBuilder(args);

// --- Veritabani (EF Core + PostgreSQL/TimescaleDB) ---
builder.Services.AddDbContext<SensorDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("SensorDb")));

// --- Repository + Service katmanlari ---
builder.Services.AddScoped<ISensorReadingRepository, SensorReadingRepository>();
builder.Services.AddScoped<ISensorReadingService, SensorReadingService>();

// --- Parser kayitlari (Strategy deseni) ---
builder.Services.AddSingleton<ISensorPayloadParser, JsonTemperatureParser>();
builder.Services.AddSingleton<ISensorPayloadParser, XmlHumidityParser>();
builder.Services.AddSingleton<ISensorPayloadParser, CsvPressureParser>();
builder.Services.AddSingleton<SensorPayloadParserFactory>();

// --- MassTransit + RabbitMQ ---
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
            // GECICI hatalar icin retry: 3 deneme, artan aralikla (1s, 2s, 5s).
            // Retry tukenirse mesaj otomatik "sensor-readings-queue_error" kuyruguna duser.
            e.UseMessageRetry(r => r.Intervals(
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5)));

            e.ConfigureConsumer<SensorRawReadingConsumer>(context);
        });
    });
});

builder.Build().Run();
