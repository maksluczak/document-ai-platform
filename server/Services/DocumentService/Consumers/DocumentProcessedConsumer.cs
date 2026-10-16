using DocumentService.Data;
using DocumentService.Models;
using Contracts.Events;
using MassTransit;

namespace DocumentService.Consumers;

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

        _logger.LogInformation("New Message Received in DocumentService");
        _logger.LogInformation("Document ID: {id}", message.DocumentId);

        var document = await _dbContext.Documents.FindAsync(message.DocumentId);
        if (document is null)
        {
            document = new ProcessedDocument
            {
                Id = message.DocumentId,
                FileName = message.FileName,
                BlobUrl = message.BlobUrl,
                UploadedAt = message.ProcessedAt
            };
            _dbContext.Documents.Add(document);
        }
        document.DocumentType = message.DocumentType;
        document.Metadata = message.ExtractedFields ?? new Dictionary<string, string>();
        document.ProcessedAt = message.ProcessedAt;
        document.DocumentStatus = Status.Completed;
        document.Error = null;

        try
        {
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Document {Id} successfully saved to database.", message.DocumentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save document {Id} to database", message.DocumentId);
            throw;
        }
    }
}