using Microsoft.AspNetCore.SignalR;
using Visitor.Web.Common.Interfaces;

namespace Visitor.Web.Infrastructure.Communication.Services;

public class VisitCommunicationService(IHubContext<VisitHub> hubContext) : IVisitCommunicationService
{
    public async Task Broadcast(string message)
    {
        await hubContext.Clients.All.SendAsync("Broadcast", message);
    }
}
