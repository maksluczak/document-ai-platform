namespace UploadService.Configuration;

public class MinIoOptions
{
    public string ServiceUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool Secure { get; set; }
}