using NavGuru.Models;

namespace NavGuru.ViewModels;

public class FaqPageViewModel
{
    public List<FaqEntry> Faqs { get; set; } = new();
    public List<string> Categories { get; set; } = new();
}