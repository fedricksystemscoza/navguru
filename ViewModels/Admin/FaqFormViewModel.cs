using System.ComponentModel.DataAnnotations;

namespace NavGuru.ViewModels.Admin;

public class FaqFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(300)]
    public string Question { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    public string Answer { get; set; } = string.Empty;

    [Required, StringLength(60)]
    public string Category { get; set; } = "General";

    [Display(Name = "Approved (visible to AI + students)")]
    public bool IsApproved { get; set; } = true;
}