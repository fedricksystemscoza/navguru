using NavGuru.Models;

namespace NavGuru.ViewModels;

public class ChecklistViewModel
{
    public Dictionary<string, List<ChecklistItem>> Items { get; set; } = new();
    public int Total { get; set; }
    public int Completed { get; set; }

    public int ProgressPercent =>
        Total == 0 ? 0 : (int)Math.Round((double)Completed / Total * 100);
}