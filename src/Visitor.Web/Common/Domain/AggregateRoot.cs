namespace Visitor.Web.Common.Domain;

public class AggregateRoot : Entity
{
    public string Description { get; private set; } = null!;

    protected AggregateRoot(Guid id, string description) : base(id)
    {
        UpdateDescription(description);
    }
    protected AggregateRoot() { }

    public virtual void UpdateDescription(string description)
    {
        Description = description;
    }
}