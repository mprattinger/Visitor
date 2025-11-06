using ErrorOr;
using FlintSoft.CQRS.Handlers;
using FlintSoft.CQRS.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Visitor.Web.Features.VisitorManagement.DomainEntities;
using Visitor.Web.Infrastructure.Persistance;

namespace Visitor.Web.Features.PlanVisit;

public static class UpdatePlannedVisit
{
    public record Command(Guid Id, string Name, string Company, DateTime? ExpectedArrival = null) : ICommand<VisitorEntity>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Visit ID is required.");

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

                var visitor = await context.Visitors
                    .FirstOrDefaultAsync(v => v.Id == command.Id, cancellationToken);

                if (visitor is null)
                {
                    return Error.NotFound("PLANNED_VISIT.NOT_FOUND", "The planned visit was not found.");
                }

                if (visitor.Status != VisitorStatus.Planned)
                {
                    return Error.Validation("PLANNED_VISIT.INVALID_STATUS", "Only planned visits can be updated.");
                }

                var visitDate = command.ExpectedArrival.HasValue
                    ? DateOnly.FromDateTime(command.ExpectedArrival.Value.ToUniversalTime())
                    : visitor.VisitDate;
                visitor.UpdatePlannedVisit(command.Name, command.Company, visitDate);

                await context.SaveChangesAsync(cancellationToken);

                return visitor;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while updating a planned visit: {err}", ex.Message);
                return Error.Failure("PLANNED_VISIT.UPDATE", "An error occurred while updating the planned visit.");
            }
        }
    }
}
