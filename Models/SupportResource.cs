using System.ComponentModel.DataAnnotations;

namespace NavGuru.Models;

public class SupportResource
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [StringLength(60)]
    public string Category { get; set; } = "General";   // e.g. "Mental Health", "Academic", "IT"

    [StringLength(500)]
    public string? Url { get; set; }

    [StringLength(200)]
    public string? ContactEmail { get; set; }

    [StringLength(50)]
    public string? ContactPhone { get; set; }

    [StringLength(200)]
    public string? Location { get; set; }

    public int Order { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}