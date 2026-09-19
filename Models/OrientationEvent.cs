using System.ComponentModel.DataAnnotations;

namespace NavGuru.Models;

public class OrientationEvent
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    [Required, StringLength(200)]
    public string Location { get; set; } = string.Empty;

    public bool IsMandatory { get; set; }

    // NEW — unique secret token used for QR check-in validation
    [StringLength(64)]
    public string CheckInToken { get; set; } = Guid.NewGuid().ToString("N");
}