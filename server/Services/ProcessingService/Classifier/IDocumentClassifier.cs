using System.Text.Json.Serialization;

namespace ProcessingService.Classifier;

public record ClassifierRequest(
    string Text
);

public record ClassificationResponse(
    [property: JsonPropertyName("document_type")] string DocumentType,
    [property: JsonPropertyName("confidence")] float Confidence,
    [property: JsonPropertyName("azure_model")] string AzureModel
);

public record ClassificationResult(
    string DocumentType,
    float Confidence,
    string AzureModel
);

public interface IDocumentClassifier
{
    public Task<ClassificationResult> ClassifyAsync(string text);
}