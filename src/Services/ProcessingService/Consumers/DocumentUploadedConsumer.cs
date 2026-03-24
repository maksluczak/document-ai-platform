using Contracts.Events;
using MassTransit;

namespace ProcessingService.Consumers;

public class DocumentUploadedConsumer : IConsumer<DocumentUploaded>
{
    private readonly ILogger<DocumentUploadedConsumer> logger;

    public DocumentUploadedConsumer(ILogger<DocumentUploadedConsumer> logger)
    {
        this.logger = logger;
    }

    public async Task Consume(ConsumeContext<DocumentUploaded> context)
    {
        var message = context.Message;

        logger.LogInformation("New Message Recieved");
        logger.LogInformation("Document ID: {id}", message.DocumentId);
        logger.LogInformation("File name: {name}", message.FileName);
        logger.LogInformation("File URL: {url}", message.BlobUrl);

        // TODO: add taking file from MinIO and sending it to AzureAI

        await Task.CompletedTask;
    }
}