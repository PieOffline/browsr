namespace ProPilot.Models;

public class Profile
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string School { get; set; } = string.Empty;
    public string GeminiApiKey { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}
