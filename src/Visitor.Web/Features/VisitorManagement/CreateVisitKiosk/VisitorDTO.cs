using System.ComponentModel.DataAnnotations;

namespace Visitor.Web.Features.VisitorManagement.CreateVisitKiosk;

public class VisitorDTO
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Bitte geben Sie Ihren Namen ein")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Bitte geben Sie den den Namen Ihrer Firma ein")]
    public string Company { get; set; } = null!;
}
