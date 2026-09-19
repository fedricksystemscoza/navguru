using System.ComponentModel.DataAnnotations;

namespace NavGuru.ViewModels;

public class SettingsViewModel
{
    [Display(Name = "Dark Mode")]
    public bool DarkMode { get; set; }

    [Display(Name = "Email Notifications")]
    public bool EmailNotifications { get; set; }

    [Display(Name = "Push Notifications")]
    public bool PushNotifications { get; set; }

    [Display(Name = "Event Reminders")]
    public bool EventReminders { get; set; }

    [Display(Name = "Offline Cache")]
    public bool OfflineCacheEnabled { get; set; }

    [Display(Name = "Language")]
    public string Language { get; set; } = "en";
}