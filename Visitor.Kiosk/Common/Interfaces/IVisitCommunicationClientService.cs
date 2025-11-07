namespace Visitor.Kiosk.Common.Interfaces;

public interface IVisitCommunicationClientService
{
    event Action<string>? OnMessageReceived;

    Task CreateHub();
    ValueTask DisposeAsync();
    Task SendMessage(string message);
}