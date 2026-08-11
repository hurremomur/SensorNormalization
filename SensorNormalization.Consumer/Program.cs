using System.Reflection;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
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
// ISensorPayloadParser'i uygulayan somut siniflar reflection ile bulunur.
// ConfigurableJsonParser haric tutulur; cunku o, parametresiz olusturulamaz
// (bir config tanimi ister) ve asagida config'den ayrica uretilir.
var parserTypes = Assembly.GetExecutingAssembly()
    .GetTypes()
    .Where(t => typeof(ISensorPayloadParser).IsAssignableFrom(t)
                && !t.IsInterface
                && !t.IsAbstract
                && t != typeof(ConfigurableJsonParser));

foreach (var type in parserTypes)
    builder.Services.AddSingleton(typeof(ISensorPayloadParser), type);

// --- Config-driven parser'lar: sensor-config.json'dan uretilir (kod yazmadan) ---
// Basit sensorler icin yeni bir parser sinifi yazmak yerine, config dosyasina
// bir tanim eklemek yeterlidir. Her tanim icin bir ConfigurableJsonParser uretilir.
var configPath = Path.Combine(AppContext.BaseDirectory, "sensor-config.json");
if (File.Exists(configPath))
{
    var json = File.ReadAllText(configPath);
    var options = JsonConvert.DeserializeObject<ConfigurableSensorOptions>(json);
    if (options is not null)
    {
        foreach (var def in options.ConfigurableSensors)
            builder.Services.AddSingleton<ISensorPayloadParser>(new ConfigurableJsonParser(def));
    }
}

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
