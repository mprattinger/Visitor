using Visitor.Web.Features.VisitorManagement.DomainEntities;

namespace Visitor.Web.Infrastructure.Persistance;

public static class DbSeeder
{
    private const int TokenLength = 16;

    public static async Task SeedAsync(VisitorDbContext context)
    {
        // Seed test visitors
        if (!context.Visitors.Any())
        {
            var visitors = new List<VisitorEntity>
            {
                VisitorEntity.CreateVisitorFromKiosk("John Doe", "Acme Corp"),
                VisitorEntity.CreateVisitorFromKiosk("Jane Smith", "TechCo"),
            };

            context.Visitors.AddRange(visitors);
            await context.SaveChangesAsync();
        }
    }
}
