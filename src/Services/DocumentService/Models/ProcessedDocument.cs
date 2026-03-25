namespace DocumentService.Models;

using System.ComponentModel.DataAnnotations;

public class ProcessedDocument
{
    [Key]
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string BlobUrl { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
    public DateTime ProcessedAt { get; set; }
}