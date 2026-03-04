namespace ProPilot.Models;

public class Assignment
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Brief { get; set; } = string.Empty;
    public string Deadline { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}
