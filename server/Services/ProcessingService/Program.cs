using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Amazon.S3;
using MassTransit;
using ProcessingService.Consumers;
using ProcessingService.Analysis;
using ProcessingService.Classifier;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton(serviceProvider =>
{
    var endpoint = builder.Configuration["AzureAi:Endpoint"];
    var apiKey = builder.Configuration["AzureAi:ApiKey"];

    if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
        throw new InvalidOperationException("Missing AzureAi:Endpoint or AzureAi:ApiKey");

    return new DocumentAnalysisClient(new Uri(endpoint.TrimEnd('/')), new AzureKeyCredential(apiKey));
});

builder.Services.AddSingleton<IAmazonS3>(serviceProvider =>
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

builder.Services.AddSingleton<IDocumentAnalysisService, DocumentAnalysisService>();

builder.Services.AddHttpClient<IDocumentClassifier, DocumentClassifier>(c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Classifier:BaseUrl"] ?? "http://localhost:8000");
    c.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddMassTransit(registration =>
{
    registration.AddConsumer<DocumentUploadedConsumer>();

    registration.UsingRabbitMq((context, rabbitMq) =>
    {
        var rabbitMqSection = builder.Configuration.GetSection("RabbitMqConnection");

        rabbitMq.Host(rabbitMqSection["Host"] ?? "localhost", "/", hostSettings =>
        {
            hostSettings.Username(rabbitMqSection["Username"] ?? "guest");
            hostSettings.Password(rabbitMqSection["Password"] ?? "guest");
        });

        rabbitMq.UseMessageRetry(retry =>
        {
            retry.Ignore<RequestFailedException>(exception => exception.Status is >= 400 and < 500 and not 429);
            retry.Exponential(5, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(5));
        });
        rabbitMq.ConfigureEndpoints(context);
    });
});

var host = builder.Build();
host.Run();