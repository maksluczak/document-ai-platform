using Amazon.S3;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Contracts.Events;
using MassTransit;
using ProcessingService.Analysis;
using ProcessingService.Classifier;

namespace ProcessingService.Consumers;

public class DocumentUploadedConsumer : IConsumer<DocumentUploaded>
{
    private const int MinTextLength = 10;

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

            using var downloadStream = new MemoryStream();
            await s3Object.ResponseStream.CopyToAsync(downloadStream);
            byte[] documentBytes = downloadStream.ToArray();

            _logger.LogInformation("Extracting text for classification...");
            using var readStream = new MemoryStream(documentBytes);
            AnalyzeResult readResult = await _analysisService.AnalyzeAsync("prebuilt-read", readStream);
            var extractedText = string.Join(
                " ",
                readResult.Pages.SelectMany(p => p.Lines).Select(l => l.Content)
            );

            ClassificationResult classificationResult;

            if (extractedText.Trim().Length < MinTextLength)
            {
                _logger.LogWarning("Document {id} has too little text to classify, treating it as 'other'", message.DocumentId);
                classificationResult = new ClassificationResult("other", 0f, "prebuilt-read");
            }
            else
            {
                _logger.LogInformation("Sending to classifier...");
                classificationResult = await _documentClassifier.ClassifyAsync(extractedText);
            }

            AnalyzeResult result;
            if (classificationResult.AzureModel == "prebuilt-read")
            {
                result = readResult;
            }
            else
            {
                _logger.LogInformation("Sending to Azure AI with model {model}...", classificationResult.AzureModel);
                using var modelStream = new MemoryStream(documentBytes);
                result = await _analysisService.AnalyzeAsync(classificationResult.AzureModel, modelStream);
            }

            var extractedData = ExtractedFields(result);
            _logger.LogInformation(
                "Document {id} classified as {type}, extracted {count} fields",
                message.DocumentId, classificationResult.DocumentType, extractedData.Count
            );

            await context.Publish(new DocumentProcessed(
                message.DocumentId,
                message.FileName,
                message.BlobUrl,
                classificationResult.DocumentType,
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

    private static Dictionary<string, string> ExtractedFields(AnalyzeResult result)
    {
        var extractedData = new Dictionary<string, string>();

        if (result.Documents.Count > 0)
        {
            foreach (var field in result.Documents[0].Fields)
            {
                extractedData[field.Key] = field.Value.Content ?? string.Empty;
            }
        }
        else
        {
            foreach (var keyValuePair in result.KeyValuePairs)
            {
                if (keyValuePair.Value is not null)
                {
                    extractedData[keyValuePair.Key.Content] = keyValuePair.Value.Content;
                }
            }
        }
        return extractedData;
    }
}
