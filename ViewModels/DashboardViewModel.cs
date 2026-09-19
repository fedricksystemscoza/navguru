using NavGuru.Models;

namespace NavGuru.ViewModels;

public class DashboardViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string? StudentNumber { get; set; }
    public string? Faculty { get; set; }

    public OrientationEvent? NextEvent { get; set; }
    public List<OrientationEvent> UpcomingEvents { get; set; } = new();

    public int ChecklistTotal { get; set; }
    public int ChecklistCompleted { get; set; }
    public List<ChecklistItem> RecentChecklist { get; set; } = new();

    public int FaqCount { get; set; }
    public int BadgesEarned { get; set; }

    public int UnreadNotifications { get; set; }
    public List<Notification> RecentNotifications { get; set; } = new();
    public int SupportResourceCount { get; set; }

    public int ProgressPercent =>
        ChecklistTotal == 0 ? 0 : (int)Math.Round((double)ChecklistCompleted / ChecklistTotal * 100);

    public TimeSpan? TimeUntilNextEvent =>
        NextEvent is null ? null : NextEvent.StartTime - DateTime.Now;
}