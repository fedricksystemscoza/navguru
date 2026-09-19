using NavGuru.Models;

namespace NavGuru.ViewModels;

public class AdminDashboardViewModel
{
    // High-level stats
    public int TotalStudents { get; set; }
    public int TotalEvents { get; set; }
    public int TotalCheckIns { get; set; }
    public int TotalFaqs { get; set; }

    public int CheckInsToday { get; set; }
    public int MandatoryEventsToday { get; set; }

    // Lists
    public List<OrientationEvent> RecentEvents { get; set; } = new();
    public List<CheckInRow> RecentCheckIns { get; set; } = new();
    public List<EventCheckInStats> EventStats { get; set; } = new();

    public double AverageAttendance =>
        TotalEvents == 0 ? 0 : Math.Round((double)TotalCheckIns / TotalEvents, 1);
}

public class CheckInRow
{
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public string EventTitle { get; set; } = string.Empty;
    public DateTime CheckedInAt { get; set; }
}

public class EventCheckInStats
{
    public int EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public string Location { get; set; } = string.Empty;
    public int CheckInCount { get; set; }
    public bool IsMandatory { get; set; }
}