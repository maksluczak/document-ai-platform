namespace UploadService.Validation;

public interface IFileValidator
{
    bool IsValidExtension(string fileName);
}