using NavGuru.Models;

namespace NavGuru.ViewModels;

public class CalendarViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public List<OrientationEvent> Events { get; set; } = new();
    public List<int> MyCheckInEventIds { get; set; } = new();

    public DateTime FirstOfMonth => new(Year, Month, 1);
    public int DaysInMonth => DateTime.DaysInMonth(Year, Month);
    public int StartWeekday => (int)FirstOfMonth.DayOfWeek;
}