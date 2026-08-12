using System.Reflection;
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

// --- Parser kayitlari: OTOMATIK KESIF (assembly scanning) ---
// ISensorPayloadParser'i uygulayan tum somut siniflar reflection ile bulunur
// ve otomatik kaydedilir. Yeni bir parser eklemek icin bu dosyaya dokunmak
// GEREKMEZ; parser sinifini yazmak yeterlidir (Open/Closed).
var parserTypes = Assembly.GetExecutingAssembly()
    .GetTypes()
    .Where(t => typeof(ISensorPayloadParser).IsAssignableFrom(t)
                && !t.IsInterface
                && !t.IsAbstract);

foreach (var type in parserTypes)
    builder.Services.AddSingleton(typeof(ISensorPayloadParser), type);

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
            e.UseMessageRetry(r => r.Intervals(
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5)));

            e.ConfigureConsumer<SensorRawReadingConsumer>(context);
        });
    });
});

builder.Build().Run();