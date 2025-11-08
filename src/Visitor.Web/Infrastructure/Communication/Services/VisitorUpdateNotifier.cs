using Visitor.Web.Common.Interfaces;

namespace Visitor.Web.Infrastructure.Communication.Services;

public class VisitorUpdateNotifier : IVisitorUpdateNotifier
{
    public event Action? OnVisitorUpdated;

    public void NotifyVisitorUpdate()
    {
        OnVisitorUpdated?.Invoke();
    }
}
