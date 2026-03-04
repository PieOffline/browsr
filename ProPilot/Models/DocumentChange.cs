namespace ProPilot.Models;

public class DocumentChange
{
    public string Original { get; set; } = string.Empty;
    public string Replacement { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public bool IsAccepted { get; set; }
    public bool IsRejected { get; set; }
}
