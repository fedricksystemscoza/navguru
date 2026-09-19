using System.ComponentModel.DataAnnotations;

namespace NavGuru.Models;

public class CampusLocation
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required, StringLength(60)]
    public string Category { get; set; } = "Building";   // Building, Service, Food, Parking, Landmark, Emergency

    [Required]
    public double Latitude { get; set; }

    [Required]
    public double Longitude { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(200)]
    public string? OpeningHours { get; set; }

    [StringLength(30)]
    public string? ContactPhone { get; set; }

    [StringLength(100)]
    public string? IconName { get; set; } = "fa-location-dot";

    public bool IsActive { get; set; } = true;

    public int Order { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}