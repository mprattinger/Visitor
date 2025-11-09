using ErrorOr;
using FlintSoft.CQRS.Handlers;
using FlintSoft.CQRS.Interfaces;
using FluentValidation;
using Visitor.Web.Common.Domains;
using Visitor.Web.Infrastructure.Persistance;

namespace Visitor.Web.Features.CheckInVisitor;

public static class CheckInVisitor
{
    public record Command(string Name, string? Company = null) : ICommand<VisitorEntity>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

            RuleFor(x => x.Company)
                .MaximumLength(100).WithMessage("Company must not exceed 100 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.Company));
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

                var visitor = new VisitorEntity(
                    name: command.Name,
                    company: string.IsNullOrWhiteSpace(command.Company) ? "Walk-in" : command.Company,
                    visitDate: DateTime.UtcNow,
                    status: VisitorStatus.Arrived
                );

                // Set arrived time since this is a check-in
                visitor.CheckIn();

                await context.Visitors.AddAsync(visitor, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);

                return visitor;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while checking in visitor: {err}", ex.Message);
                return Error.Failure("KIOSK.VISITOR.CHECKIN", "An error occurred while checking in the visitor.");
            }
        }
    }
}
