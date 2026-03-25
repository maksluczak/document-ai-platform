namespace DocumentService.Consumers;

using DocumentService.Data;
using DocumentService.Models;
using Contracts.Events;
using MassTransit;

public class DocumentProcessedConsumer : IConsumer<DocumentProcessed>
{
    private readonly ILogger<DocumentProcessedConsumer> _logger;
    private readonly DocumentDbContext _dbContext;

    public DocumentProcessedConsumer(ILogger<DocumentProcessedConsumer> logger, DocumentDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<DocumentProcessed> context)
    {
        var message = context.Message;

        _logger.LogInformation("New Message Recieved");
        _logger.LogInformation("Document ID: {id}", message.DocumentId);
        _logger.LogInformation("File name: {name}", message.FileName);
        _logger.LogInformation("File URL: {url}", message.BlobUrl);

        var document = new ProcessedDocument
        {
            Id = message.DocumentId,
            FileName = message.FileName,
            BlobUrl = message.BlobUrl,
            DocumentType = message.DocumentType,
            Metadata = message.ExtractedFields,
            ProcessedAt = message.ProcessedAt
        };
        _dbContext.Documents.Add(document);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Document {Id} saved to database.", message.DocumentId);
    }
}