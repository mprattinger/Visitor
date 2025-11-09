namespace Visitor.Web.Common.Interfaces;

public interface IVisitorUpdateNotifier
{
    event Action? OnVisitorUpdated;
    void NotifyVisitorUpdate();
}
