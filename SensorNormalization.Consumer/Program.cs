using MassTransit;
using SensorNormalization.Consumer.Consumers;

var builder = Host.CreateApplicationBuilder(args);

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
