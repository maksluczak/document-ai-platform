using MassTransit;
using ProcessingService.Consumers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<DocumentUploadedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/");

        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();
host.Run();
