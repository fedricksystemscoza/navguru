using System.ComponentModel.DataAnnotations;

namespace NavGuru.ViewModels.Admin;

public class ResourceFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required, StringLength(60)]
    public string Category { get; set; } = "General";

    [Url, StringLength(500)]
    public string? Url { get; set; }

    [EmailAddress, StringLength(200)]
    [Display(Name = "Contact Email")]
    public string? ContactEmail { get; set; }

    [StringLength(50)]
    [Display(Name = "Contact Phone")]
    public string? ContactPhone { get; set; }

    [StringLength(200)]
    public string? Location { get; set; }

    public int Order { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}