namespace Visitor.Shared.Common.Interfaces;

public interface IVisitCommunicationService
{
    event Action<string>? OnMessageReceived;

    Task CreateHub();
    ValueTask DisposeAsync();
    Task SendMessage(string message);
}
