using DocumentService.Data;
using Contracts.Events;
using MassTransit;
using DocumentService.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentService.Consumers;

public class DocumentUploadedStatusConsumer : IConsumer<DocumentUploaded>
{
    private readonly ILogger<DocumentUploadedStatusConsumer> _logger;
    private readonly DocumentDbContext _dbContext;

    public DocumentUploadedStatusConsumer(ILogger<DocumentUploadedStatusConsumer> logger, DocumentDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<DocumentUploaded> context)
    {
        var message = context.Message;
        bool documentExists = await _dbContext.Documents.AnyAsync(document => document.Id == message.DocumentId);
        if (documentExists)
        {
            _logger.LogInformation("Document {Id} already exists, skipping", message.DocumentId);
            return;
        }
        var document = new ProcessedDocument
        {
            Id = message.DocumentId,
            FileName = message.FileName,
            BlobUrl = message.BlobUrl,
            DocumentStatus = Status.Processing,
            UploadedAt = message.UploadedAt
        };
        _dbContext.Documents.Add(document);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Document {Id} saved with status Processing", message.DocumentId);
    }
}