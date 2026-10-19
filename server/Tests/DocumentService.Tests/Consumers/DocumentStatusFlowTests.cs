using Contracts.Events;
using DocumentService.Consumers;
using DocumentService.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DocumentService.Tests.Consumers;

[Collection("Postgres")]
public class DocumentStatusFlowTests
{
    private readonly PostgresFixture _fixture;

    private readonly DocumentUploaded _uploadedEvent = new(
        Guid.NewGuid(),
        "invoice.pdf",
        "https://blob.local/invoice.pdf",
        DateTime.UtcNow);

    public DocumentStatusFlowTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task ConsumeUploadedAsync(DocumentUploaded uploadedEvent)
    {
        var context = new Mock<ConsumeContext<DocumentUploaded>>();
        context.Setup(consumeContext => consumeContext.Message).Returns(uploadedEvent);

        await using var dbContext = _fixture.CreateDbContext();
        var consumer = new DocumentUploadedStatusConsumer(NullLogger<DocumentUploadedStatusConsumer>.Instance, dbContext);
        await consumer.Consume(context.Object);
    }

    private async Task<ProcessedDocument?> LoadDocumentAsync(Guid documentId)
    {
        await using var dbContext = _fixture.CreateDbContext();
        return await dbContext.Documents.FindAsync(documentId);
    }

    private DocumentProcessed CreateProcessedEvent() => new(
        _uploadedEvent.DocumentId,
        _uploadedEvent.FileName,
        _uploadedEvent.BlobUrl,
        "invoice",
        new Dictionary<string, string> { ["Total"] = "1234.56 PLN" },
        DateTime.UtcNow
    );

    private async Task ConsumeProcessedAsync(DocumentProcessed processedEvent)
    {
        var context = new Mock<ConsumeContext<DocumentProcessed>>();
        context.Setup(consumeContext => consumeContext.Message).Returns(processedEvent);

        await using var dbContext = _fixture.CreateDbContext();
        var consumer = new DocumentProcessedConsumer(NullLogger<DocumentProcessedConsumer>.Instance, dbContext);
        await consumer.Consume(context.Object);
    }

    private async Task ConsumeFaultAsync(DocumentUploaded uploadedEvent, string errorMessage)
    {
        var exceptionInfo = new Mock<ExceptionInfo>();
        exceptionInfo.Setup(info => info.Message).Returns(errorMessage);

        var fault = new Mock<Fault<DocumentUploaded>>();
        fault.Setup(faultMessage => faultMessage.Message).Returns(uploadedEvent);
        fault.Setup(faultMessage => faultMessage.Exceptions).Returns([exceptionInfo.Object]);

        var context = new Mock<ConsumeContext<Fault<DocumentUploaded>>>();
        context.Setup(consumeContext => consumeContext.Message).Returns(fault.Object);

        await using var dbContext = _fixture.CreateDbContext();
        var consumer = new DocumentUploadedFaultConsumer(NullLogger<DocumentUploadedFaultConsumer>.Instance, dbContext);
        await consumer.Consume(context.Object);
    }

    private async Task<int> CountDocumentsAsync(Guid documentId)
    {
        await using var dbContext = _fixture.CreateDbContext();
        return await dbContext.Documents.CountAsync(document => document.Id == documentId);
    }

    [Fact]
    public async Task UploadedConsumer_CreatesProcessingDocument_WhenDocumentIsNew()
    {
        await ConsumeUploadedAsync(_uploadedEvent);

        var document = await LoadDocumentAsync(_uploadedEvent.DocumentId);
        Assert.NotNull(document);
        Assert.Equal(Status.Processing, document.DocumentStatus);
        Assert.Equal(_uploadedEvent.UploadedAt, document.UploadedAt, TimeSpan.FromMilliseconds(1));
        Assert.Null(document.ProcessedAt);
        Assert.Null(document.Error);
    }

    [Fact]
    public async Task UploadedConsumer_DoesNotOverwrite_WhenDocumentAlreadyExists()
    {
        var processedEvent = CreateProcessedEvent();
        await ConsumeProcessedAsync(processedEvent);
        await ConsumeUploadedAsync(_uploadedEvent);

        var document = await LoadDocumentAsync(_uploadedEvent.DocumentId);
        Assert.NotNull(document);
        Assert.Equal(Status.Completed, document.DocumentStatus);
        Assert.Equal(1, await CountDocumentsAsync(_uploadedEvent.DocumentId));
    }

    [Fact]
    public async Task ProcessedConsumer_MarksDocumentCompleted_WithTypeAndMetadata()
    {
        var processedEvent = CreateProcessedEvent();
        await ConsumeUploadedAsync(_uploadedEvent);
        await ConsumeProcessedAsync(processedEvent);

        var document = await LoadDocumentAsync(_uploadedEvent.DocumentId);
        Assert.NotNull(document);
        Assert.Equal(Status.Completed, document.DocumentStatus);
        Assert.Equal("invoice", document.DocumentType);
        Assert.Equal("1234.56 PLN", document.Metadata["Total"]);
        Assert.NotNull(document.ProcessedAt);
        Assert.Equal(processedEvent.ProcessedAt, document.ProcessedAt.Value, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task ProcessedConsumer_IsIdempotent_WhenMessageIsDeliveredTwice()
    {
        var processedEvent = CreateProcessedEvent();
        await ConsumeProcessedAsync(processedEvent);
        await ConsumeProcessedAsync(processedEvent);

        var document = await LoadDocumentAsync(_uploadedEvent.DocumentId);
        Assert.NotNull(document);
        Assert.Equal(Status.Completed, document.DocumentStatus);
        Assert.Equal(1, await CountDocumentsAsync(_uploadedEvent.DocumentId));
    }

    [Fact]
    public async Task ProcessedConsumer_CreatesCompletedDocument_WhenUploadedEventIsLate()
    {
        var processedEvent = CreateProcessedEvent();
        await ConsumeProcessedAsync(processedEvent);
        await ConsumeUploadedAsync(_uploadedEvent);

        var document = await LoadDocumentAsync(_uploadedEvent.DocumentId);
        Assert.NotNull(document);
        Assert.Equal(Status.Completed, document.DocumentStatus);
        Assert.Equal("invoice", document.DocumentType);
    }

    [Fact]
    public async Task ProcessedConsumer_ClearsError_WhenDocumentSucceedsAfterFailure()
    {
        await ConsumeUploadedAsync(_uploadedEvent);
        await ConsumeFaultAsync(_uploadedEvent, "");
        var processedEvent = CreateProcessedEvent();
        await ConsumeProcessedAsync(processedEvent);

        var document = await LoadDocumentAsync(_uploadedEvent.DocumentId);
        Assert.NotNull(document);
        Assert.Equal(Status.Completed, document.DocumentStatus);
        Assert.Null(document.Error);
    }

    [Fact]
    public async Task FaultConsumer_MarksDocumentFailed_WithErrorMessage()
    {
        await ConsumeUploadedAsync(_uploadedEvent);
        await ConsumeFaultAsync(_uploadedEvent, "Azure returned 401");

        var document = await LoadDocumentAsync(_uploadedEvent.DocumentId);
        Assert.NotNull(document);
        Assert.Equal(Status.Failed, document.DocumentStatus);
        Assert.Equal("Azure returned 401", document.Error);
    }

    [Fact]
    public async Task FaultConsumer_TruncatesErrorMessage_To1000Characters()
    {
        await ConsumeUploadedAsync(_uploadedEvent);
        await ConsumeFaultAsync(_uploadedEvent, new string('x', 1500));

        var document = await LoadDocumentAsync(_uploadedEvent.DocumentId);
        Assert.NotNull(document);
        Assert.Equal(Status.Failed, document.DocumentStatus);
        Assert.NotNull(document?.Error);
        Assert.Equal(1000, document.Error.Length);
    }

    [Fact]
    public async Task FaultConsumer_DoesNotOverwriteCompletedDocument()
    {
        await ConsumeUploadedAsync(_uploadedEvent);
        var processedEvent = CreateProcessedEvent();
        await ConsumeProcessedAsync(processedEvent);
        await ConsumeFaultAsync(_uploadedEvent, "");

        var document = await LoadDocumentAsync(_uploadedEvent.DocumentId);
        Assert.NotNull(document);
        Assert.Equal(Status.Completed, document.DocumentStatus);
        Assert.Equal(1, await CountDocumentsAsync(_uploadedEvent.DocumentId));
        Assert.Null(document.Error);
    }

    [Fact]
    public async Task Migrations_AreUpToDateWithModel()
    {
        await using var dbContext = _fixture.CreateDbContext();

        Assert.False(dbContext.Database.HasPendingModelChanges());
    }
}