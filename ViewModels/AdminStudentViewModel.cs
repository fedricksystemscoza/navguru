namespace NavGuru.ViewModels;

public class AdminStudentViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? StudentNumber { get; set; }
    public string? Faculty { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsAdmin { get; set; }

    public int ChecklistTotal { get; set; }
    public int ChecklistCompleted { get; set; }
    public int CheckInCount { get; set; }

    public int ProgressPercent =>
        ChecklistTotal == 0 ? 0 : (int)Math.Round((double)ChecklistCompleted / ChecklistTotal * 100);
}