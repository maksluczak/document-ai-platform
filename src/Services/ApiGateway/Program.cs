var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
});

var app = builder.Build();

app.MapReverseProxy();

app.Run();
