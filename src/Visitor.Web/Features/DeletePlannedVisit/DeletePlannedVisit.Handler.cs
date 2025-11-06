using ErrorOr;
using FlintSoft.CQRS.Handlers;
using FlintSoft.CQRS.Interfaces;
using Microsoft.EntityFrameworkCore;
using Visitor.Web.Features.VisitorManagement.DomainEntities;
using Visitor.Web.Infrastructure.Persistance;

namespace Visitor.Web.Features.DeletePlannedVisit;

public static class DeletePlannedVisit
{
    public record Command(Guid Id) : ICommand<bool>;

    internal sealed class Handler(ILogger<Handler> logger, VisitorDbContext context) : ICommandHandler<Command, bool>
    {
        public async Task<ErrorOr<bool>> Handle(Command command, CancellationToken cancellationToken)
        {
            try
            {
                var visitor = await context.Visitors
                    .FirstOrDefaultAsync(v => v.Id == command.Id, cancellationToken);

                if (visitor is null)
                {
                    return Error.NotFound("PLANNED_VISIT.NOT_FOUND", "The planned visit was not found.");
                }

                if (visitor.Status != VisitorStatus.Planned)
                {
                    return Error.Validation("PLANNED_VISIT.INVALID_STATUS", "Only planned visits can be deleted.");
                }

                context.Visitors.Remove(visitor);
                await context.SaveChangesAsync(cancellationToken);

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while deleting a planned visit: {err}", ex.Message);
                return Error.Failure("PLANNED_VISIT.DELETE", "An error occurred while deleting the planned visit.");
            }
        }
    }
}
