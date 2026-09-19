namespace NavGuru.ViewModels;

public class CheckInHistoryRow
{
    public string EventTitle { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime CheckedInAt { get; set; }
    public bool IsMandatory { get; set; }
}