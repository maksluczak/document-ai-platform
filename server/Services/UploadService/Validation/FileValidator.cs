namespace UploadService.Validation;

public class FileValidator
{
    private static readonly string[] AllowedExtensions = { ".pdf" };

    public bool IsValidExtension(string filename)
    {
        string extension = Path.GetExtension(filename).ToLower();
        return AllowedExtensions.Contains(extension);
    }
}