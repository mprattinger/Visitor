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
            var visitors = new List<Features.VisitorManagement.DomainEntities.VisitorEntity>
            {
                new Features.VisitorManagement.DomainEntities.VisitorEntity(
                    Guid.NewGuid(),
                    "John Doe",
                    "Acme Corp",
                    TimeSpan.FromHours(2),
                    GenerateToken())
                {
                    Status = VisitorStatus.Planned,
                    CreatedAt = DateTime.UtcNow
                },
                new Features.VisitorManagement.DomainEntities.VisitorEntity(
                    Guid.NewGuid(),
                    "Jane Smith",
                    "TechCo",
                    TimeSpan.FromHours(1),
                    GenerateToken())
                {
                    Status = VisitorStatus.Arrived,
                    CreatedAt = DateTime.UtcNow.AddHours(-1),
                    ArrivedAt = DateTime.UtcNow.AddMinutes(-30)
                }
            };

            context.Visitors.AddRange(visitors);
            await context.SaveChangesAsync();
        }
    }

    private static string GenerateToken()
    {
        return Guid.NewGuid().ToString("N")[..TokenLength].ToUpper();
    }
}
