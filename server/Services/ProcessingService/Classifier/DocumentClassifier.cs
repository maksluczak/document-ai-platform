using System.Net.Http.Json;

namespace ProcessingService.Classifier;

public class DocumentClassifier : IDocumentClassifier
{
    private readonly HttpClient _http;
    private readonly ILogger<DocumentClassifier> _logger;

    public DocumentClassifier(HttpClient http, ILogger<DocumentClassifier> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<ClassificationResult> ClassifyAsync(string text)
    {
        var truncatedText = text.Length < 2000 ? text : text[..2000];
        var payload = new ClassifierRequest(truncatedText);

        var response = await _http.PostAsJsonAsync("/classify", payload);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ClassificationResponse>() ?? throw new InvalidOperationException("Failed to deserialize classification response.");

        if (result!.Confidence < 0.6f)
        {
            _logger.LogWarning("Low confidence {conf} for doc, falling back to 'other'", result.Confidence);
            return new ClassificationResult("other", result.Confidence, result.AzureModel);
        }

        return new ClassificationResult(result.DocumentType, result.Confidence, result.AzureModel);
    }
}

