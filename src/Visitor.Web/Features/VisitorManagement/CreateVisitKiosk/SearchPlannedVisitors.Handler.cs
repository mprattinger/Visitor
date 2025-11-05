using ErrorOr;
using FlintSoft.CQRS.Handlers;
using FlintSoft.CQRS.Interfaces;
using Microsoft.EntityFrameworkCore;
using Visitor.Web.Features.VisitorManagement.DomainEntities;
using Visitor.Web.Infrastructure.Persistance;

namespace Visitor.Web.Features.VisitorManagement.CreateVisitKiosk;

public static class SearchPlannedVisitors
{
    public record Query(string SearchTerm) : IQuery<List<VisitorSearchResultDTO>>;

    internal sealed class Handler(VisitorDbContext context) : IQueryHandler<Query, List<VisitorSearchResultDTO>>
    {
        public async Task<ErrorOr<List<VisitorSearchResultDTO>>> Handle(Query query, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query.SearchTerm) || query.SearchTerm.Length < 3)
                {
                    return new List<VisitorSearchResultDTO>();
                }

                var searchTermLower = query.SearchTerm.ToLower();
                
                var visitors = await context.Visitors
                    .Where(v => v.Status == VisitorStatus.Planned && 
                                v.Name.ToLower().Contains(searchTermLower))
                    .OrderBy(v => v.Name)
                    .Select(v => new VisitorSearchResultDTO(v.Id, v.Name, v.Company))
                    .Take(10) // Limit results to 10
                    .ToListAsync(cancellationToken);

                return visitors;
            }
            catch (Exception)
            {
                return new List<VisitorSearchResultDTO>();
            }
        }
    }
}

public record VisitorSearchResultDTO(Guid Id, string Name, string Company);
