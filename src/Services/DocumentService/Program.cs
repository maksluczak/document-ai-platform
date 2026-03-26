using DocumentService.Data;
using Microsoft.EntityFrameworkCore;
using MassTransit;
using DocumentService.Consumers;
using dotenv.net;
using Npgsql;

DotEnv.Load();

var builder = Host.CreateApplicationBuilder(args);

var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION");

builder.Services.AddDbContext<DocumentDbContext>(options =>
{
    var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION");

    var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
    dataSourceBuilder.EnableDynamicJson();
    var dataSource = dataSourceBuilder.Build();

    options.UseNpgsql(dataSource);
});

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<DocumentProcessedConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/");
        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();
host.Run();