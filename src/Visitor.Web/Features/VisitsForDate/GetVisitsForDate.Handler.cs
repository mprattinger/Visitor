using ErrorOr;
using FlintSoft.CQRS.Handlers;
using FlintSoft.CQRS.Interfaces;
using Microsoft.EntityFrameworkCore;
using Visitor.Web.Common.Domains;
using Visitor.Web.Infrastructure.Persistance;

namespace Visitor.Web.Features.VisitsForDate;

public static class GetVisitsForDate
{
    public record Query(DateTime ForDate) : IQuery<DashboardDataDTO>;

    internal sealed class Handler(VisitorDbContext context) : IQueryHandler<Query, DashboardDataDTO>
    {
        public async Task<ErrorOr<DashboardDataDTO>> Handle(Query query, CancellationToken cancellationToken)
        {
            try
            {
                var forDate = query.ForDate.Date.ToUniversalTime();
                var nextDate = forDate.AddDays(1);

                var plannedVisitors = await context.Visitors
                    .Where(v => v.Status == VisitorStatus.Planned && v.VisitDate >= forDate && v.VisitDate < nextDate)
                    .OrderBy(v => v.CreatedAt)
                    .Select(v => new VisitorSummaryDTO(v.Id, v.Name, v.Company, v.CreatedAt, v.ArrivedAt, v.LeftAt, v.Status))
                    .ToListAsync(cancellationToken);

                var currentlyVisiting = await context.Visitors
                    .Where(v => v.Status == VisitorStatus.Arrived && v.ArrivedAt >= forDate && v.ArrivedAt < nextDate)
                    .OrderBy(v => v.ArrivedAt)
                    .Select(v => new VisitorSummaryDTO(v.Id, v.Name, v.Company, v.CreatedAt, v.ArrivedAt, v.LeftAt, v.Status))
                    .ToListAsync(cancellationToken);

                var alreadyLeft = await context.Visitors
                    .Where(v => v.Status == VisitorStatus.Left && v.LeftAt >= forDate && v.LeftAt < nextDate)
                    .OrderBy(v => v.LeftAt)
                    .Select(v => new VisitorSummaryDTO(v.Id, v.Name, v.Company, v.CreatedAt, v.ArrivedAt, v.LeftAt, v.Status))
                    .ToListAsync(cancellationToken);

                return new DashboardDataDTO(plannedVisitors, currentlyVisiting, alreadyLeft);
            }
            catch (Exception ex)
            {
                return Error.Failure("DASHBOARD.GET", $"An error occurred while fetching dashboard data: {ex.Message}");
            }
        }
    }
}

public record DashboardDataDTO(
    List<VisitorSummaryDTO> PlannedVisitors,
    List<VisitorSummaryDTO> CurrentlyVisiting,
    List<VisitorSummaryDTO> AlreadyLeft
);

public record VisitorSummaryDTO(
    Guid Id,
    string Name,
    string Company,
    DateTime CreatedAt,
    DateTime? ArrivedAt,
    DateTime? LeftAt,
    VisitorStatus Status
);
