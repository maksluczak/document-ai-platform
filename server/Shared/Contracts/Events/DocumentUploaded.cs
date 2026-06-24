namespace Contracts.Events
{
    public record DocumentUploaded(
        Guid DocumentId,
        string FileName,
        string BlobUrl,
        DateTime UploadedAt)
    {
        public DocumentUploaded() : this(Guid.Empty, string.Empty, string.Empty, DateTime.MinValue) { }
    }
}