using ErrorOr;
using FlintSoft.CQRS.Handlers;
using FlintSoft.CQRS.Interfaces;
using FluentValidation;
using Visitor.Web.Features.VisitorManagement.DomainEntities;
using Visitor.Web.Infrastructure.Persistance;

namespace Visitor.Web.Features.PlanVisit;

public static class CreatePlannedVisit
{
    public record Command(string Name, string Company, DateTime? ExpectedArrival = null) : ICommand<VisitorEntity>;

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

                var arrival = DateOnly.FromDateTime(DateTime.UtcNow);
                if (command.ExpectedArrival is not null)
                {
                    arrival = DateOnly.FromDateTime(command.ExpectedArrival.Value!.ToUniversalTime());
                }

                var visitor = new VisitorEntity(command.Name, command.Company, arrival, VisitorStatus.Planned);

                await context.Visitors.AddAsync(visitor, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);

                return visitor;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while creating a planned visit: {err}", ex.Message);
                return Error.Failure("DASHBOARD.PLANNED_VISIT.CREATE", "An error occurred while creating the planned visit.");
            }
        }
    }
}
