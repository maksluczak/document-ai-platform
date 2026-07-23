namespace ProcessingService.Classifier;

public record ClassifierRequest(
    string Text
);

public record ClassificationResponse(
    string DocumentType,
    float Confidence,
    string AzureModel
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