using System.ComponentModel.DataAnnotations;

namespace Visitor.Kiosk.Features.CheckIn;

public class CheckInModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [MaxLength(100, ErrorMessage = "Name must not exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100, ErrorMessage = "Company must not exceed 100 characters")]
    public string? Company { get; set; }
}
