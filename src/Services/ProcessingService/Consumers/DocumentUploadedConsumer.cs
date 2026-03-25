namespace ProcessingService.Consumers;

using Amazon.S3;
using Azure.AI.DocumentIntelligence;
using Contracts.Events;
using MassTransit;
using Azure;

public class DocumentUploadedConsumer : IConsumer<DocumentUploaded>
{
    private readonly ILogger<DocumentUploadedConsumer> _logger;
    private readonly IAmazonS3 _s3Client;
    private readonly DocumentIntelligenceClient _azureClient;

    public DocumentUploadedConsumer(ILogger<DocumentUploadedConsumer> logger, IAmazonS3 s3Client, DocumentIntelligenceClient azureClient)
    {
        _logger = logger;
        _s3Client = s3Client;
        _azureClient = azureClient;
    }

    public async Task Consume(ConsumeContext<DocumentUploaded> context)
    {
        var message = context.Message;

        _logger.LogInformation("New Message Recieved");
        _logger.LogInformation("Document ID: {id}", message.DocumentId);
        _logger.LogInformation("File name: {name}", message.FileName);
        _logger.LogInformation("File URL: {url}", message.BlobUrl);

        try
        {
            var objectKey = $"{message.DocumentId}-{message.FileName}";
            var s3Object = await _s3Client.GetObjectAsync(
                "documents",
                objectKey
            );
            using var stream = s3Object.ResponseStream;

            var analyzeContent = new AnalyzeDocumentContent
            {
                Base64Source = await BinaryData.FromStreamAsync(stream)
            };

            _logger.LogInformation("Sending to Azure AI: {name}", message.FileName);
            var operation = await _azureClient.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                "prebuilt-invoice",
                analyzeContent
            );

            var result = operation.Value;

            foreach (var document in result.Documents)
            {
                _logger.LogInformation("AI Found document of type: {type}", document.DocType);

                if (document.Fields.TryGetValue("VendorName", out var vendor))
                    _logger.LogInformation("Vendor: {vendor}", vendor.ValueString);

                if (document.Fields.TryGetValue("InvoiceTotal", out var total))
                    _logger.LogInformation("Total: {total}", total.ValueCurrency?.Amount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while processing document {id}", message.DocumentId);
            throw;
        }
    }
}
