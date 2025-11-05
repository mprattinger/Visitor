using ErrorOr;
using FlintSoft.CQRS.Handlers;
using FlintSoft.CQRS.Interfaces;
using Microsoft.EntityFrameworkCore;
using Visitor.Web.Features.VisitorManagement.DomainEntities;
using Visitor.Web.Infrastructure.Persistance;

namespace Visitor.Web.Features.DashboardHome;

public static class GetDashboardData
{
    public record Query : IQuery<DashboardDataDTO>;

    internal sealed class Handler(VisitorDbContext context) : IQueryHandler<Query, DashboardDataDTO>
    {
        public async Task<ErrorOr<DashboardDataDTO>> Handle(Query query, CancellationToken cancellationToken)
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var tomorrow = today.AddDays(1);

                var plannedVisitors = await context.Visitors
                    .Where(v => v.Status == VisitorStatus.Planned && v.CreatedAt >= today && v.CreatedAt < tomorrow)
                    .OrderBy(v => v.CreatedAt)
                    .Select(v => new VisitorSummaryDTO(v.Id, v.Name, v.Company, v.CreatedAt, v.ArrivedAt, v.LeftAt))
                    .ToListAsync(cancellationToken);

                var currentlyVisiting = await context.Visitors
                    .Where(v => v.Status == VisitorStatus.Arrived && v.ArrivedAt >= today && v.ArrivedAt < tomorrow)
                    .OrderBy(v => v.ArrivedAt)
                    .Select(v => new VisitorSummaryDTO(v.Id, v.Name, v.Company, v.CreatedAt, v.ArrivedAt, v.LeftAt))
                    .ToListAsync(cancellationToken);

                var alreadyLeft = await context.Visitors
                    .Where(v => v.Status == VisitorStatus.Left && v.LeftAt >= today && v.LeftAt < tomorrow)
                    .OrderBy(v => v.LeftAt)
                    .Select(v => new VisitorSummaryDTO(v.Id, v.Name, v.Company, v.CreatedAt, v.ArrivedAt, v.LeftAt))
                    .ToListAsync(cancellationToken);

                return new DashboardDataDTO(plannedVisitors, currentlyVisiting, alreadyLeft);
            }
            catch (Exception ex)
            {
                return Error.Failure("DASHBOARD.DATA.FETCH", $"An error occurred while fetching dashboard data: {ex.Message}");
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
    DateTime? LeftAt
);
