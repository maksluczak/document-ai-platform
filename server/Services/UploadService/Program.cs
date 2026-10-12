using Amazon.S3;
using MassTransit;
using UploadService.Configuration;
using UploadService.Validation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.Configure<MinIoOptions>(builder.Configuration.GetSection("MinIOConnection"));

builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var options = builder.Configuration.GetSection("MinIOConnection").Get<MinIoOptions>();
    var clientConfig = new AmazonS3Config
    {
        ServiceURL = options!.ServiceUrl,
        ForcePathStyle = true,
        UseHttp = !options.Secure
    };

    return new AmazonS3Client(options.Username, options.Password, clientConfig);
});

builder.Services.AddMassTransit(registration =>
{
    registration.UsingRabbitMq((context, rabbitMq) =>
    {
        var rabbitMqSection = builder.Configuration.GetSection("RabbitMqConnection");

        rabbitMq.Host(rabbitMqSection["Host"] ?? "localhost", "/", hostSettings =>
        {
            hostSettings.Username(rabbitMqSection["Username"] ?? "guest");
            hostSettings.Password(rabbitMqSection["Password"] ?? "guest");
        });
    });
});

builder.Services.AddSingleton<IFileValidator, FileValidator>();

var app = builder.Build();

const string bucketName = "documents";
var s3Client = app.Services.GetRequiredService<IAmazonS3>();

for (var attempt = 1; ; attempt++)
{
    try
    {
        await s3Client.EnsureBucketExistsAsync(bucketName);
        break;
    }
    catch (Exception) when (attempt < 10)
    {
        await Task.Delay(TimeSpan.FromSeconds(2));
    }
}

app.MapControllers();
app.Run();