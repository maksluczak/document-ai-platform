using DocumentService.Data;
using Contracts.Events;
using DocumentService.Models;

using MassTransit;

public class DocumentUploadedFaultConsumer : IConsumer<Fault<DocumentUploaded>>
{
    private const int MaxErrorLength = 1000;

    private readonly ILogger<DocumentUploadedFaultConsumer> _logger;
    private readonly DocumentDbContext _dbContext;

    public DocumentUploadedFaultConsumer(ILogger<DocumentUploadedFaultConsumer> logger, DocumentDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<Fault<DocumentUploaded>> context)
    {
        var originalMessage = context.Message.Message;
        string errorMessage = context.Message.Exceptions.FirstOrDefault()?.Message ?? "Unknown error";
        if (errorMessage.Length > MaxErrorLength)
        {
            errorMessage = errorMessage[..MaxErrorLength];
        }

        var document = await _dbContext.Documents.FindAsync(originalMessage.DocumentId);
        if (document is null)
        {
            document = new ProcessedDocument
            {
                Id = originalMessage.DocumentId,
                FileName = originalMessage.FileName,
                BlobUrl = originalMessage.BlobUrl,
                UploadedAt = originalMessage.UploadedAt
            };
            _dbContext.Documents.Add(document);
        }

        if (document.DocumentStatus == Status.Completed)
        {
            _logger.LogInformation("Document {Id} is already completed, ignoring fault", originalMessage.DocumentId);
            return;
        }

        document.DocumentStatus = Status.Failed;
        document.Error = errorMessage;
        await _dbContext.SaveChangesAsync();

        _logger.LogWarning("Document {Id} marked as failed: {Error}", originalMessage.DocumentId, errorMessage);
    }
}