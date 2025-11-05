using ErrorOr;
using FlintSoft.CQRS.Handlers;
using FlintSoft.CQRS.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Visitor.Web.Features.VisitorManagement.DomainEntities;
using Visitor.Web.Infrastructure.Persistance;

namespace Visitor.Web.Features.VisitorManagement.CreateVisitKiosk;

public static class CreateVisitKiosk
{
    public record Command(string Name, string Company, Guid? PlannedVisitorId = null) : ICommand<VisitorEntity>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

            RuleFor(x => x.Company)
                .NotEmpty().WithMessage("Company is required.")
                .MaximumLength(100).WithMessage("Company must not exceed 100 characters.");
        }
    }

    internal sealed class Handler(ILogger<Handler> logger, IValidator<Command> validator, VisitorDbContext context) : ICommandHandler<Command, VisitorEntity>
    {
        public async Task<ErrorOr<VisitorEntity>> Handle(Command command, CancellationToken cancellationToken)
        {
            try
            {
                var validation = await validator.ValidateAsync(command, cancellationToken);
                if (!validation.IsValid)
                {
                    return validation.Errors.ConvertAll(error => Error.Validation(error.PropertyName, error.ErrorMessage));
                }

                VisitorEntity? visit;

                // Check if this is a planned visitor checking in
                if (command.PlannedVisitorId is not null)
                {
                    visit = await context.Visitors
                        .FirstOrDefaultAsync(v => v.Id == command.PlannedVisitorId && v.Status == VisitorStatus.Planned, cancellationToken);

                    if (visit != null)
                    {
                        // Update the existing planned visitor to arrived status
                        visit.CheckIn();
                        context.Visitors.Update(visit);
                    }
                    else
                    {
                        // Planned visitor not found, create new visitor
                        visit = new VisitorEntity(command.Name, command.Company, DateOnly.FromDateTime(DateTime.UtcNow), VisitorStatus.Arrived);
                        await context.Visitors.AddAsync(visit, cancellationToken);
                    }
                }
                else
                {
                    // No planned visitor ID, create new visitor
                    visit = new VisitorEntity(command.Name, command.Company, DateOnly.FromDateTime(DateTime.UtcNow), VisitorStatus.Arrived);
                    await context.Visitors.AddAsync(visit, cancellationToken);
                }

                await context.SaveChangesAsync(cancellationToken);

                return visit;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while creating the visit at the kiosk: {err}", ex.Message);
                return Error.Failure("KIOSK.VISIT.CREATE", "An error occurred while creating the visit at the kiosk.");
            }
        }
    }
}
