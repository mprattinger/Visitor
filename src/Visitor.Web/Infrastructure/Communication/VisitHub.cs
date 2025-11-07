using Microsoft.AspNetCore.SignalR;

namespace Visitor.Web.Infrastructure.Communication;

public class VisitHub : Hub
{
    public async Task Broadcast(string message)
    {
        await Clients.All.SendAsync("Broadcast", message);
    }
}
