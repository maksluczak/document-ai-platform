using Testcontainers.PostgreSql;
using DocumentService.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DocumentService.Tests;

public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:17")
        .WithDatabase("DocumentDb")
        .WithUsername("admin")
        .WithPassword("password")
        .Build();

    private NpgsqlDataSource? _dataSource;
    public string DatabaseConnectionString => _dbContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(DatabaseConnectionString);
        dataSourceBuilder.EnableDynamicJson();
        _dataSource = dataSourceBuilder.Build();

        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_dataSource is not null)
            await _dataSource.DisposeAsync();

        await _dbContainer.DisposeAsync();
    }

    public DocumentDbContext CreateDbContext()
    {
        if (_dataSource is null)
            throw new InvalidOperationException("PostgresFixture has not been initialized.");

        var options = new DbContextOptionsBuilder<DocumentDbContext>()
            .UseNpgsql(_dataSource)
            .Options;

        return new DocumentDbContext(options);
    }
}