using System.ComponentModel.DataAnnotations;
using Visitor.Web.Common.Domain;

namespace Visitor.Web.Features.VisitorManagement.DomainEntities;

public class VisitorEntity : AggregateRoot
{
    [Required]
    [MaxLength(100)]
    public string Name { get; private set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Company { get; private set; } = string.Empty;

    public VisitorStatus Status { get; private set; } = VisitorStatus.Planned;

    public DateTime CreatedAt { get; private set; }

    public DateTime? ArrivedAt { get; private set; }

    public DateTime? LeftAt { get; private set; }

    public string? CreatedByEntraId { get; private set; }

    protected VisitorEntity() : base() { }

    public VisitorEntity(string name, string company, VisitorStatus status, Guid? id = null)
        : base(id ?? Guid.CreateVersion7())
    {
        Name = name;
        Company = company;
        CreatedAt = DateTime.UtcNow;
        Status = status;
    }

    public static VisitorEntity CreateVisitorFromKiosk(string name, string company)
    {
        return new VisitorEntity(name, company, VisitorStatus.Arrived);
    }
}
