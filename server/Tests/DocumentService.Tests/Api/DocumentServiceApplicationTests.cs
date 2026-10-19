using System.Net;
using System.Text.Json;
using Contracts.Events;
using DocumentService.Data;
using DocumentService.Models;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace DocumentService.Tests.Api;

[Collection("Postgres")]
public class DocumentServiceApplicationTests
{
    private readonly PostgresFixture _fixture;

    public DocumentServiceApplicationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetDocuments_ReturnsEmptyList_OnFreshDatabase()
    {
        var freshDatabase = new NpgsqlConnectionStringBuilder(_fixture.DatabaseConnectionString)
        {
            Database = $"fresh_{Guid.NewGuid():N}"
        };
        await using var factory = new DocumentServiceFactory(freshDatabase.ConnectionString);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/documents");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetDocuments_ReturnsStatusAsText()
    {
        var documentId = Guid.NewGuid();
        await using (var dbContext = _fixture.CreateDbContext())
        {
            dbContext.Documents.Add(new ProcessedDocument
            {
                Id = documentId,
                FileName = "broken.pdf",
                DocumentStatus = Status.Failed,
                Error = "Azure returned 401",
                UploadedAt = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync();
        }
        await using var factory = new DocumentServiceFactory(_fixture.DatabaseConnectionString);
        var client = factory.CreateClient();

        var json = await client.GetStringAsync("/api/documents");

        using var body = JsonDocument.Parse(json);
        var document = body.RootElement.EnumerateArray()
            .Single(element => element.GetProperty("id").GetGuid() == documentId);
        Assert.Equal("Failed", document.GetProperty("documentStatus").GetString());
        Assert.Equal("Azure returned 401", document.GetProperty("error").GetString());
    }

    [Fact]
    public async Task DocumentUploaded_CreatesProcessingDocument_ThroughTheBus()
    {
        var uploadedEvent = new DocumentUploaded(Guid.NewGuid(), "invoice.pdf", "https://blob.local/invoice.pdf", DateTime.UtcNow);
        await using var factory = new DocumentServiceFactory(_fixture.DatabaseConnectionString);
        var harness = factory.Services.GetRequiredService<ITestHarness>();
        await harness.Start();

        await harness.Bus.Publish(uploadedEvent);

        Assert.True(await harness.Consumed.Any<DocumentUploaded>());
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DocumentDbContext>();
        var document = await dbContext.Documents.FindAsync(uploadedEvent.DocumentId);
        Assert.NotNull(document);
        Assert.Equal(Status.Processing, document.DocumentStatus);
    }
}
