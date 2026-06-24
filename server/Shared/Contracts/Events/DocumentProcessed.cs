namespace Contracts.Events
{
    public record DocumentProcessed(
        Guid DocumentId,
        string FileName,
        string BlobUrl,
        string DocumentType,
        Dictionary<string, string> ExtractedFields,
        DateTime ProcessedAt
    );
}