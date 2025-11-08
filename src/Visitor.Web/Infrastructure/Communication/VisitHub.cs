using Microsoft.AspNetCore.SignalR;

namespace Visitor.Web.Infrastructure.Communication;

public class VisitHub : Hub
{
    public async Task Hello(string msg)
    {
        Console.WriteLine(msg);
    }
}
