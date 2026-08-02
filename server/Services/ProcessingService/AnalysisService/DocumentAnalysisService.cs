using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;

namespace ProcessingService.Analysis;

public class DocumentAnalysisService : IDocumentAnalysisService
{
    private readonly DocumentAnalysisClient _client;

    public DocumentAnalysisService(DocumentAnalysisClient client)
    {
        _client = client;
    }

    public async Task<AnalyzeResult> AnalyzeAsync(string modelId, Stream document)
    {
        Operation<AnalyzeResult> operation = await _client.AnalyzeDocumentAsync(
            WaitUntil.Completed,
            modelId,
            document);

        return operation.Value;
    }
}