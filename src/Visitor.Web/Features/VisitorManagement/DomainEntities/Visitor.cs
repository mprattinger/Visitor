using System.ComponentModel.DataAnnotations;
using Visitor.Web.Common.Domain;

namespace Visitor.Web.Features.VisitorManagement.DomainEntities;

public class Visitor : AggregateRoot
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Company { get; set; } = string.Empty;
    
    [Required]
    public TimeSpan PlannedDuration { get; set; }
    
    public VisitorStatus Status { get; set; } = VisitorStatus.Planned;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? ArrivedAt { get; set; }
    
    public DateTime? LeftAt { get; set; }
    
    public string? CreatedByEntraId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string VisitorToken { get; set; } = string.Empty;
    
    protected Visitor() : base() { }
    
    public Visitor(Guid id, string name, string company, TimeSpan plannedDuration, string visitorToken)
        : base(id, $"{name} from {company}")
    {
        Name = name;
        Company = company;
        PlannedDuration = plannedDuration;
        VisitorToken = visitorToken;
        CreatedAt = DateTime.UtcNow;
        Status = VisitorStatus.Planned;
    }
}
