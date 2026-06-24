namespace UploadService.DTOs
{
    public record UploadResponse(
        Guid DocumentId,
        string FileName
    );
}