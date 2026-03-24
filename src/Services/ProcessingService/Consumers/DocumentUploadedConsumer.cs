namespace ProcessingService.Consumers;

using Contracts.Events;
using MassTransit;

public class DocumentUploadedConsumer : IConsumer<DocumentUploaded>
{
    private readonly ILogger<DocumentUploadedConsumer> _logger;

    public DocumentUploadedConsumer(ILogger<DocumentUploadedConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DocumentUploaded> context)
    {
        var message = context.Message;

        _logger.LogInformation("New Message Recieved");
        _logger.LogInformation("Document ID: {id}", message.DocumentId);
        _logger.LogInformation("File name: {name}", message.FileName);
        _logger.LogInformation("File URL: {url}", message.BlobUrl);

        // TODO: add taking file from MinIO and sending it to AzureAI

        await Task.CompletedTask;
    }
}
