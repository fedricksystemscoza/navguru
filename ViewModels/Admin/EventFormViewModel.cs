using System.ComponentModel.DataAnnotations;

namespace NavGuru.ViewModels.Admin;

public class EventFormViewModel
{
    public int Id { get; set; }   // 0 = create, >0 = edit

    [Required, StringLength(150)]
    [Display(Name = "Event Title")]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Start Time")]
    public DateTime StartTime { get; set; } = DateTime.Today.AddDays(1).AddHours(9);

    [Required]
    [Display(Name = "End Time")]
    public DateTime EndTime { get; set; } = DateTime.Today.AddDays(1).AddHours(10);

    [Required, StringLength(200)]
    public string Location { get; set; } = string.Empty;

    [Display(Name = "Mandatory for students")]
    public bool IsMandatory { get; set; }
}