using Amazon.S3;
using Azure;
using Azure.AI.DocumentIntelligence;
using MassTransit;
using ProcessingService.Consumers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var config = builder.Configuration.GetSection("MinIOConnection");
    var s3Config = new AmazonS3Config
    {
        ServiceURL = config["ServiceUrl"],
        ForcePathStyle = true,
        UseHttp = !bool.Parse(config["Secure"] ?? "false")
    };

    return new AmazonS3Client(config["Username"], config["Password"], s3Config);
});

builder.Services.AddSingleton(sp =>
{
    var config = builder.Configuration.GetSection("AzureAI");

    return new DocumentIntelligenceClient(
        new Uri(config["Endpoint"]!),
        new AzureKeyCredential(config["ApiKey"]!)
    );
});

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
