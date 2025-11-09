using FlintSoft.CQRS.Handlers;
using Microsoft.AspNetCore.SignalR;
using Visitor.Web.Common.Domains;
using Visitor.Web.Common.Interfaces;

namespace Visitor.Web.Infrastructure.Communication;

public class VisitHub(ICommandHandler<Features.CheckInVisitor.CheckInVisitor.Command, VisitorEntity> checkInHandler, ILogger<VisitHub> logger, IVisitorUpdateNotifier updateNotifier) : Hub
{
    public async Task Hello(string msg)
    {
        Console.WriteLine(msg);
    }

    public async Task VisitorCheckIn(string name, string company)
    {
        try
        {
            logger.LogInformation("Visitor check-in received: {Name}, {Company}", name, company);

            // Use the command handler with validation
            var result = await checkInHandler.Handle(
                new Features.CheckInVisitor.CheckInVisitor.Command(name, company),
                CancellationToken.None
            );

            if (result.IsError)
            {
                var firstError = result.Errors.FirstOrDefault();
                var errorMessage = firstError.Description ?? "Validation failed";
                logger.LogWarning("Visitor check-in validation failed: {Error}", errorMessage);
                throw new ArgumentException(errorMessage);
            }

            var visitor = result.Value;
            logger.LogInformation("Visitor checked in successfully: {VisitorId}", visitor.Id);

            // Notify dashboard components to refresh
            updateNotifier.NotifyVisitorUpdate();

            // Broadcast update to all connected clients (kiosks)
            await Clients.All.SendAsync("Broadcast", $"New visitor checked in: {name}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing visitor check-in for {Name}", name);
            throw;
        }
    }
}
