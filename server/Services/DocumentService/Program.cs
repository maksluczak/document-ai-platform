using DocumentService.Data;
using DocumentService.Consumers;
using Microsoft.EntityFrameworkCore;
using MassTransit;
using Npgsql;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var connectionString = builder.Configuration.GetConnectionString("PostgresDb")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:PostgresDb");

builder.Services.AddNpgsqlDataSource(
    connectionString,
    dataSourceBuilder => dataSourceBuilder.EnableDynamicJson());

builder.Services.AddDbContext<DocumentDbContext>((serviceProvider, options) =>
    options.UseNpgsql(serviceProvider.GetRequiredService<NpgsqlDataSource>()));

builder.Services.AddMassTransit(registration =>
{
    registration.AddConsumer<DocumentProcessedConsumer>();
    registration.AddConsumer<DocumentUploadedStatusConsumer>();
    registration.AddConsumer<DocumentUploadedFaultConsumer>();

    registration.UsingRabbitMq((context, rabbitMq) =>
    {
        var rabbitMqSection = builder.Configuration.GetSection("RabbitMqConnection");

        rabbitMq.Host(rabbitMqSection["Host"] ?? "localhost", "/", hostSettings =>
        {
            hostSettings.Username(rabbitMqSection["Username"] ?? "guest");
            hostSettings.Password(rabbitMqSection["Password"] ?? "guest");
        });
        rabbitMq.UseMessageRetry(retry => retry.Interval(3, TimeSpan.FromSeconds(2)));
        rabbitMq.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<DocumentDbContext>().Database.MigrateAsync();

app.MapControllers();
app.Run();

public partial class Program;