using Visitor.Kiosk.Features.CheckIn;
using Visitor.Shared.DTOs;

namespace Visitor.Kiosk.Common.Interfaces;

public interface IVisitCommunicationClientService
{
    event Action<CommunicationDTO>? OnMessageReceived;

    Task CreateHub();
    ValueTask DisposeAsync();
    Task SendMessage(string message);
    Task SendVisitorCheckIn(Guid id, string name, string company, CheckinMode mode);
}
