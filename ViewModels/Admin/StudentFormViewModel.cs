using System.ComponentModel.DataAnnotations;

namespace NavGuru.ViewModels.Admin;

public class StudentFormViewModel
{
    public string? Id { get; set; }   // null = create

    [Required, StringLength(120)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [StringLength(30)]
    [Display(Name = "Student Number")]
    public string? StudentNumber { get; set; }

    [StringLength(80)]
    public string? Faculty { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string? Password { get; set; }   // required only on create

    [Display(Name = "Assign Admin role")]
    public bool IsAdmin { get; set; }
}