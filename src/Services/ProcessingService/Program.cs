using Amazon.S3;
using Azure;
using Azure.AI.DocumentIntelligence;
using dotenv.net;
using MassTransit;
using ProcessingService.Consumers;

DotEnv.Load();

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddSingleton(sp =>
{
    var key = Environment.GetEnvironmentVariable("AZURE_AI_KEY")
                    ?? builder.Configuration["AzureAi:ApiKey"];
    var endpoint = Environment.GetEnvironmentVariable("AZURE_AI_ENDPOINT")
                    ?? builder.Configuration["AzureAi:Endpoint"];

    return new DocumentIntelligenceClient(new Uri(endpoint!), new AzureKeyCredential(key!));
});

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
