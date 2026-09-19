
using Microsoft.AspNetCore.Identity;

namespace NavGuru.Models;

public class FaqEntry
{
    public int Id { get; set; }
    public string Question { get; set; } = "";
    public string Answer { get; set; } = "";
    public string Category { get; set; } = "";
    public bool IsApproved { get; set; }
}
