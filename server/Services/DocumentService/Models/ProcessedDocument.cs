namespace DocumentService.Models;

using System.ComponentModel.DataAnnotations;

public class ProcessedDocument
{
    [Key]
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string BlobUrl { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public Status DocumentStatus { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
    public DateTime UploadedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? Error { get; set; }
}

public enum Status
{
    Processing = 0,
    Completed = 1,
    Failed = 2
}
