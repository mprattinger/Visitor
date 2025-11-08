using Microsoft.AspNetCore.SignalR;
using Visitor.Web.Common.Domains;
using Visitor.Web.Common.Interfaces;
using Visitor.Web.Infrastructure.Persistance;

namespace Visitor.Web.Infrastructure.Communication;

public class VisitHub(VisitorDbContext dbContext, ILogger<VisitHub> logger, IVisitorUpdateNotifier updateNotifier) : Hub
{
    public async Task Hello(string msg)
    {
        Console.WriteLine(msg);
    }

    public async Task VisitorCheckIn(string name, string company)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(name))
            {
                logger.LogWarning("Visitor check-in rejected: Name is required");
                throw new ArgumentException("Name is required", nameof(name));
            }

            if (name.Length > 100)
            {
                logger.LogWarning("Visitor check-in rejected: Name exceeds maximum length");
                throw new ArgumentException("Name must not exceed 100 characters", nameof(name));
            }

            if (!string.IsNullOrWhiteSpace(company) && company.Length > 100)
            {
                logger.LogWarning("Visitor check-in rejected: Company exceeds maximum length");
                throw new ArgumentException("Company must not exceed 100 characters", nameof(company));
            }

            logger.LogInformation("Visitor check-in received: {Name}, {Company}", name, company);

            // Create new visitor entity
            var visitor = new VisitorEntity(
                name: name,
                company: string.IsNullOrWhiteSpace(company) ? "Walk-in" : company,
                visitDate: DateTime.UtcNow,
                status: VisitorStatus.Arrived
            );

            // Set arrived time since this is a check-in
            visitor.CheckIn();

            // Save to database
            await dbContext.Visitors.AddAsync(visitor);
            await dbContext.SaveChangesAsync();

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
