using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MassTransit;

namespace DocumentService.Tests.Api;

public class DocumentServiceFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public DocumentServiceFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:PostgresDb", _connectionString);
        builder.ConfigureServices(services => services.AddMassTransitTestHarness());
    }
}