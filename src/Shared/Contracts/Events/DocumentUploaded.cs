namespace Contracts.Events
{
    public record DocumentUploaded(
        Guid DocumentId,
        string FileName,
        string BlobUrl,
        DateTime UploadedAt
    );
}