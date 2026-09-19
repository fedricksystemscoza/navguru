using System.ComponentModel.DataAnnotations;

namespace NavGuru.ViewModels.Admin;

public class NotificationFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(1000)]
    public string Message { get; set; } = string.Empty;

    [Display(Name = "Send to all students")]
    public bool Broadcast { get; set; } = true;

    [Display(Name = "Publish now")]
    public bool PublishNow { get; set; } = true;
}