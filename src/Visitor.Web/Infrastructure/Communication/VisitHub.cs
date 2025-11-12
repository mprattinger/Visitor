using FlintSoft.CQRS.Handlers;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using Visitor.Shared.DTOs;
using Visitor.Web.Common.Domains;
using Visitor.Web.Common.Interfaces;
using Visitor.Web.Features.CheckInVisitor;
using Visitor.Web.Features.VisitorInfo;

namespace Visitor.Web.Infrastructure.Communication;

public class VisitHub(ICommandHandler<CheckInVisitor.Command, VisitorEntity> checkInHandler, ICommandHandler<MarkVisitorArrived.Command, VisitorEntity> markVisitorArrivedCommandHandler, ILogger<VisitHub> logger, IVisitorUpdateNotifier updateNotifier) : Hub
{
    public async Task Hello(string msg)
    {
        Console.WriteLine(msg);
    }

    public async Task VisitorCheckIn(string payload)
    {
        try
        {
            var dto = JsonSerializer.Deserialize<CommunicationDTO>(payload);
            if (dto == null)
            {
                throw new ArgumentException("Invalid payload");
            }

            if (dto.Mode == "SELF_CHECK_IN")
            {
                logger.LogInformation("Visitor check-in received: {Name}, {Company}", dto.Name, dto.Company);

                // Use the command handler with validation
                var result = await checkInHandler.Handle(
                    new CheckInVisitor.Command(dto.Name, dto.Company ?? ""),
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
            }

            if (dto.Mode == "REMOTE_CHECK_IN")
            {
                var result = await markVisitorArrivedCommandHandler.Handle(new MarkVisitorArrived.Command(dto.Id), CancellationToken.None);

                if (result.IsError)
                {
                    var firstError = result.Errors.FirstOrDefault();
                    var errorMessage = firstError.Description ?? "Validation failed";
                    logger.LogWarning("Visitor check-in validation failed: {Error}", errorMessage);
                    throw new ArgumentException(errorMessage);
                }
            }

            // Notify dashboard components to refresh
            updateNotifier.NotifyVisitorUpdate();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing visitor check-in for {payload}", payload);
            throw;
        }
    }
}
