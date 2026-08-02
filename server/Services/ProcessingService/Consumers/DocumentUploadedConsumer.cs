using Amazon.S3;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Contracts.Events;
using MassTransit;
using ProcessingService.Analysis;
using ProcessingService.Classifier;

namespace ProcessingService.Consumers;

public class DocumentUploadedConsumer : IConsumer<DocumentUploaded>
{
    private readonly ILogger<DocumentUploadedConsumer> _logger;
    private readonly IAmazonS3 _s3Client;
    private readonly IDocumentAnalysisService _analysisService;
    private readonly IDocumentClassifier _documentClassifier;

    public DocumentUploadedConsumer(
        ILogger<DocumentUploadedConsumer> logger,
        IAmazonS3 s3Client,
        IDocumentAnalysisService analysisService,
        IDocumentClassifier documentClassifier)
    {
        _logger = logger;
        _s3Client = s3Client;
        _analysisService = analysisService;
        _documentClassifier = documentClassifier;
    }

    public async Task Consume(ConsumeContext<DocumentUploaded> context)
    {
        var message = context.Message;
        _logger.LogInformation("Starting processing with FormRecognizer: {name}", message.FileName);

        try
        {
            var objectKey = $"{message.DocumentId}-{message.FileName}";
            var s3Object = await _s3Client.GetObjectAsync("documents", objectKey);

            using var memoryStream = new MemoryStream();
            await s3Object.ResponseStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            _logger.LogInformation("Extracting text for classification...");
            AnalyzeResult readResult = await _analysisService.AnalyzeAsync("prebuilt-read", memoryStream);
            var extractedText = string.Join(
                " ",
                readResult.Pages.SelectMany(p => p.Lines).Select(l => l.Content));

            _logger.LogInformation("Sending to classifier...");
            ClassificationResult classificationResult = await _documentClassifier.ClassifyAsync(extractedText);

            AnalyzeResult result;
            if (classificationResult.AzureModel == "prebuilt-read")
            {
                result = readResult;
            }
            else
            {
                _logger.LogInformation("Sending to Azure AI with model {model}...", classificationResult.AzureModel);
                memoryStream.Position = 0;
                result = await _analysisService.AnalyzeAsync(classificationResult.AzureModel, memoryStream);
            }

            var extractedData = new Dictionary<string, string>();
            string docType = classificationResult.DocumentType;

            if (result.Documents.Count > 0)
            {
                var doc = result.Documents[0];
                docType = doc.DocumentType;

                foreach (var field in doc.Fields)
                {
                    extractedData[field.Key] = field.Value.Content;
                    _logger.LogInformation("Field: {key} = {value}", field.Key, field.Value.Content);
                }
            }
            else
            {
                _logger.LogWarning("No structured document found. Extracting raw lines...");
                foreach (var page in result.Pages)
                {
                    foreach (var line in page.Lines)
                    {
                        _logger.LogInformation("Raw text: {text}", line.Content);
                    }
                }
            }

            await context.Publish(new DocumentProcessed(
                message.DocumentId,
                message.FileName,
                message.BlobUrl,
                docType,
                extractedData,
                DateTime.UtcNow
            ));

            _logger.LogInformation("Successfully published DocumentProcessed for: {id}", message.DocumentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in FormRecognizer Consumer: {id}", message.DocumentId);
            throw;
        }
    }
}
