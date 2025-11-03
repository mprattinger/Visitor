namespace Visitor.Web.Common.Domain;

public class AggregateRoot : Entity
{
    protected AggregateRoot(Guid id) : base(id)
    {

    }
    protected AggregateRoot() { }
}