using System.ComponentModel.DataAnnotations;

namespace NavGuru.ViewModels;

public class ProfileViewModel
{
    // ---- Read-only display (NEVER bound from the form) ----
    public string? FullName { get; set; }
    public string? StudentNumber { get; set; }
    public string? Email { get; set; }
    public string? RoleName { get; set; }
    public DateTime CreatedAt { get; set; }

    [Phone]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Faculty")]
    public string? Faculty { get; set; }

    [Display(Name = "Preferred Language")]
    public string Language { get; set; } = "en";

    [Display(Name = "Email me about new events")]
    public bool EmailNotifications { get; set; } = true;

    [Display(Name = "Push notifications")]
    public bool PushNotifications { get; set; } = true;

    [Display(Name = "Event reminders")]
    public bool EventReminders { get; set; } = true;

    [Display(Name = "Dark mode")]
    public bool DarkMode { get; set; }
}