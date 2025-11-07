namespace Visitor.Web.Common.Interfaces;

public interface IVisitCommunicationService
{
    Task Broadcast(string message);
}
