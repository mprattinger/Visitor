using ErrorOr;
using FlintSoft.CQRS.Handlers;
using FlintSoft.CQRS.Interfaces;
using Microsoft.EntityFrameworkCore;
using Visitor.Web.Common.Domains;
using Visitor.Web.Infrastructure.Persistance;

namespace Visitor.Web.Features.VisitorInfo;

public static class VisitorLeaves
{
    public record Command(Guid VisitorId) : ICommand<VisitorEntity>;

    internal sealed class Handler(ILogger<Handler> logger, VisitorDbContext context) : ICommandHandler<Command, VisitorEntity>
    {
        public async Task<ErrorOr<VisitorEntity>> Handle(Command command, CancellationToken cancellationToken)
        {
            try
            {
                var visitor = await context.Visitors
                    .FirstOrDefaultAsync(v => v.Id == command.VisitorId, cancellationToken);

                if (visitor == null)
                {
                    return Error.NotFound("VISITOR.NOT_FOUND", "Visitor not found.");
                }

                if (visitor.Status != VisitorStatus.Arrived)
                {
                    return Error.Validation("VISITOR.INVALID_STATUS", "Visitor must be in arrived status to mark as left.");
                }

                visitor.Leave();
                context.Visitors.Update(visitor);
                await context.SaveChangesAsync(cancellationToken);

                return visitor;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while marking visitor as left: {err}", ex.Message);
                return Error.Failure("VISITOR.LEAVES", "An error occurred while marking visitor as left.");
            }
        }
    }
}
