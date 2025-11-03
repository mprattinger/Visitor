using System;
using ErrorOr;
using FlintSoft.CQRS.Handlers;
using FlintSoft.CQRS.Interfaces;
using FluentValidation;
using Visitor.Web.Features.VisitorManagement.DomainEntities;
using Visitor.Web.Infrastructure.Persistance;

namespace Visitor.Web.Features.VisitorManagement.CreateVisitKiosk;

public static class CreateVisitKiosk
{
    public record Command(string Name, string Company) : ICommand<VisitorEntity>;

    protected sealed class Validator : AbstractValidator<Command>
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

    internal sealed class Handler(Logger<Handler> logger, IValidator<Command> validator, VisitorDbContext context) : ICommandHandler<Command, VisitorEntity>
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

                var visit = VisitorEntity.CreateVisitorFromKiosk(command.Name, command.Company);
                await context.Visitors.AddAsync(visit);
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
