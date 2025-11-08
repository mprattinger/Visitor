using ErrorOr;
using FlintSoft.CQRS.Handlers;
using FlintSoft.CQRS.Interfaces;
using Microsoft.EntityFrameworkCore;
using Visitor.Web.Common.Domains;
using Visitor.Web.Infrastructure.Persistance;

namespace Visitor.Web.Features.VisitManagement;

public static class GetVisitsForMonth
{
    public record Query(int Year, int Month) : IQuery<List<MonthVisitDTO>>;

    internal sealed class Handler(VisitorDbContext context) : IQueryHandler<Query, List<MonthVisitDTO>>
    {
        public async Task<ErrorOr<List<MonthVisitDTO>>> Handle(Query query, CancellationToken cancellationToken)
        {
            try
            {
                var startDate = new DateTime(query.Year, query.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var endDate = startDate.AddMonths(1);

                var visits = await context.Visitors
                    .Where(v => v.Status == VisitorStatus.Planned
                        && v.VisitDate >= startDate
                        && v.VisitDate < endDate)
                    .OrderBy(v => v.VisitDate)
                    .ThenBy(v => v.CreatedAt)
                    .Select(v => new MonthVisitDTO(
                        v.Id,
                        v.Name,
                        v.Company,
                        v.VisitDate,
                        v.CreatedAt))
                    .ToListAsync(cancellationToken);

                return visits;
            }
            catch (Exception ex)
            {
                return Error.Failure("PLANNED_VISITS.GET_MONTH", $"An error occurred while fetching month visits: {ex.Message}");
            }
        }
    }
}

public record MonthVisitDTO(
    Guid Id,
    string Name,
    string Company,
    DateTime VisitDate,
    DateTime CreatedAt
);
