using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Amazon.S3;
using dotenv.net;
using MassTransit;
using ProcessingService.Consumers;
using ProcessingService.Analysis;
using ProcessingService.Classifier;

DotEnv.Load();
var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddSingleton(sp =>
{
    var key = builder.Configuration["AZURE_AI_KEY"];
    var endpoint = builder.Configuration["AZURE_AI_ENDPOINT"]?.TrimEnd('/');

    return new DocumentAnalysisClient(new Uri(endpoint!), new AzureKeyCredential(key!));
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

builder.Services.AddSingleton<IDocumentAnalysisService, DocumentAnalysisService>();

builder.Services.AddHttpClient<IDocumentClassifier, DocumentClassifier>(c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Classifier:BaseUrl"] ?? "http://localhost:8000");
    c.Timeout = TimeSpan.FromSeconds(30);
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