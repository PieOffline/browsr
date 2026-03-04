namespace ProPilot.Models;

public class Document
{
    public long Id { get; set; }
    public long? AssignmentId { get; set; }
    public string Filename { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty; // "pdf" or "docx"
    public string CreatedAt { get; set; } = string.Empty;
}
