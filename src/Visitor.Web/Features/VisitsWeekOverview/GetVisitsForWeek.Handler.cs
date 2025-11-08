using ErrorOr;
using FlintSoft.CQRS.Handlers;
using FlintSoft.CQRS.Interfaces;
using Microsoft.EntityFrameworkCore;
using Visitor.Web.Common.Domains;
using Visitor.Web.Infrastructure.Persistance;

namespace Visitor.Web.Features.VisitsWeekOverview;

public static class GetVisitsForWeek
{
    public record Query : IQuery<List<NextWorkweekVisitDTO>>;

    internal sealed class Handler(VisitorDbContext context) : IQueryHandler<Query, List<NextWorkweekVisitDTO>>
    {
        public async Task<ErrorOr<List<NextWorkweekVisitDTO>>> Handle(Query query, CancellationToken cancellationToken)
        {
            try
            {
                var (start, end) = Extensions.GetWeek();

                var ret = new List<NextWorkweekVisitDTO>();

                var plannedVisits = await context.Visitors
                    .Where(v => v.Status == VisitorStatus.Planned
                        && v.VisitDate >= start
                        && v.VisitDate <= end)
                    .OrderBy(v => v.VisitDate)
                    .ThenBy(v => v.CreatedAt)
                    .ToListAsync(cancellationToken);

                while (start <= end)
                {
                    var visits = plannedVisits.Where(x => x.VisitDate.Date == start.Date).Select(v => new VistsAtDateDTO(
                        v.Id,
                        v.Name,
                        v.Company,
                        v.VisitDate.ToLocalTime(),
                        v.CreatedAt)).ToList();

                    ret.Add(new NextWorkweekVisitDTO(start, visits));

                    start = start.AddDays(1);
                }

                return ret;
            }
            catch (Exception ex)
            {
                return Error.Failure("PLANNED_VISITS.GET_NEXT_WORKWEEK", $"An error occurred while fetching next workweek visits: {ex.Message}");
            }
        }
    }
}

public record NextWorkweekVisitDTO(
    DateTime Date,
    List<VistsAtDateDTO> Visits
);
public record VistsAtDateDTO(
    Guid Id,
    string Name,
    string Company,
    DateTime VisitDate,
    DateTime CreatedAt
);