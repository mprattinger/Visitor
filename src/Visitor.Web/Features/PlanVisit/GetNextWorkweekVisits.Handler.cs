using ErrorOr;
using FlintSoft.CQRS.Handlers;
using FlintSoft.CQRS.Interfaces;
using Microsoft.EntityFrameworkCore;
using Visitor.Web.Features.VisitorManagement.DomainEntities;
using Visitor.Web.Infrastructure.Persistance;

namespace Visitor.Web.Features.PlanVisit;

public static class GetNextWorkweekVisits
{
    public record Query : IQuery<List<NextWorkweekVisitDTO>>;

    internal sealed class Handler(VisitorDbContext context) : IQueryHandler<Query, List<NextWorkweekVisitDTO>>
    {
        public async Task<ErrorOr<List<NextWorkweekVisitDTO>>> Handle(Query query, CancellationToken cancellationToken)
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var currentDayOfWeek = (int)today.DayOfWeek;
                
                // Calculate days until next Monday
                // If today is Sunday (0), next Monday is 1 day away
                // If today is Monday (1), next Monday is 7 days away
                // If today is Tuesday (2), next Monday is 6 days away, etc.
                int daysUntilNextMonday;
                if (currentDayOfWeek == 0) // Sunday
                {
                    daysUntilNextMonday = 1;
                }
                else if (currentDayOfWeek == 1) // Monday
                {
                    daysUntilNextMonday = 7;
                }
                else
                {
                    daysUntilNextMonday = 8 - currentDayOfWeek;
                }

                var nextMonday = today.AddDays(daysUntilNextMonday);
                var nextMondayDateOnly = DateOnly.FromDateTime(nextMonday);
                var nextFridayDateOnly = nextMondayDateOnly.AddDays(4); // Monday + 4 = Friday
                var followingSaturday = nextFridayDateOnly.AddDays(1);

                var plannedVisits = await context.Visitors
                    .Where(v => v.Status == VisitorStatus.Planned 
                        && v.VisitDate >= nextMondayDateOnly 
                        && v.VisitDate <= nextFridayDateOnly)
                    .OrderBy(v => v.VisitDate)
                    .ThenBy(v => v.CreatedAt)
                    .Select(v => new NextWorkweekVisitDTO(
                        v.Id, 
                        v.Name, 
                        v.Company, 
                        v.VisitDate,
                        v.CreatedAt))
                    .ToListAsync(cancellationToken);

                return plannedVisits;
            }
            catch (Exception ex)
            {
                return Error.Failure("PLANNED_VISITS.GET_NEXT_WORKWEEK", $"An error occurred while fetching next workweek visits: {ex.Message}");
            }
        }
    }
}

public record NextWorkweekVisitDTO(
    Guid Id,
    string Name,
    string Company,
    DateOnly VisitDate,
    DateTime CreatedAt
);
