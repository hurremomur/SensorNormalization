using MassTransit;
using Microsoft.EntityFrameworkCore;
using SensorNormalization.Consumer.Application.Parsers;
using SensorNormalization.Consumer.Application.Repositories;
using SensorNormalization.Consumer.Application.Services.Abstract;
using SensorNormalization.Consumer.Application.Services.Concrete;
using SensorNormalization.Consumer.Consumers;
using SensorNormalization.Consumer.Infrastructure.Contexts;

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
            e.ConfigureConsumer<SensorRawReadingConsumer>(context);
        });
    });
});

builder.Build().Run();
