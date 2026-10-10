using DocumentService.Data;
using DocumentService.Consumers;
using Microsoft.EntityFrameworkCore;
using MassTransit;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
var connectionString = builder.Configuration.GetConnectionString("PostgresDb")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:PostgresDb");

builder.Services.AddNpgsqlDataSource(
    connectionString,
    dataSourceBuilder => dataSourceBuilder.EnableDynamicJson());

builder.Services.AddDbContext<DocumentDbContext>((serviceProvider, options) =>
    options.UseNpgsql(serviceProvider.GetRequiredService<NpgsqlDataSource>()));

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<DocumentProcessedConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/");
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<DocumentDbContext>().Database.MigrateAsync();

app.MapControllers();
app.Run();