using Azure.AI.FormRecognizer.DocumentAnalysis;

namespace ProcessingService.Analysis;

public interface IDocumentAnalysisService
{
    Task<AnalyzeResult> AnalyzeAsync(string modelId, Stream document);
}