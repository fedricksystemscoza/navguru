using System.ComponentModel.DataAnnotations;

namespace NavGuru.ViewModels.Admin;

public class CampusLocationFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required, StringLength(60)]
    public string Category { get; set; } = "Building";

    [Required, Range(-90, 90)]
    public double Latitude { get; set; }

    [Required, Range(-180, 180)]
    public double Longitude { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(200)]
    [Display(Name = "Opening Hours")]
    public string? OpeningHours { get; set; }

    [StringLength(30)]
    [Display(Name = "Contact Phone")]
    public string? ContactPhone { get; set; }

    [StringLength(100)]
    [Display(Name = "Font Awesome Icon (e.g. fa-book)")]
    public string? IconName { get; set; } = "fa-location-dot";

    public int Order { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}