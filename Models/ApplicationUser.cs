namespace NavGuru.Models;

using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? StudentNumber { get; set; }
    public string? Faculty { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ═══ NEW: User preferences ═══
    public bool DarkMode { get; set; } = false;
    public bool EmailNotifications { get; set; } = true;
    public bool PushNotifications { get; set; } = true;
    public bool EventReminders { get; set; } = true;
    public string Language { get; set; } = "en";          
    public bool OfflineCacheEnabled { get; set; } = true;
}