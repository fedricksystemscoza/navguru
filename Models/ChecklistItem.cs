using Microsoft.AspNetCore.Identity;

namespace NavGuru.Models;

public class ChecklistItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Category { get; set; } = "";
    public int Order { get; set; }
    public bool IsComplete { get; set; }
    public string UserId { get; set; } = "";
}
