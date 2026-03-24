using Amazon.S3;
using MassTransit;
using UploadService.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

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

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var host = builder.Configuration.GetValue<string>("RabbitMqConnection:Host");
        cfg.Host(host, "/");
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();