using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Visitor.Web.Features.VisitorManagement.DomainEntities;

namespace Visitor.Web.Infrastructure.Persistance;

public static class DbSeeder
{
    private const int TokenLength = 16;

    public static async Task SeedAsync(VisitorDbContext context, ILogger? logger = null)
    {
        try
        {
            // Check if any visitors exist using a direct SQL query to avoid EF Core model cache issues
            var connection = context.Database.GetDbConnection();
            await connection.OpenAsync();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(*) FROM Visitors";
                var result = await command.ExecuteScalarAsync();
                var count = Convert.ToInt64(result);

                if (count == 0)
                {
                    var now = DateTime.UtcNow;
                    var today = now.Date;

                    var visitors = new List<VisitorEntity>
                    {
                        // Planned visitors - not yet arrived
                        new VisitorEntity("Max Mustermann", "Tech Solutions GmbH", DateTime.UtcNow, VisitorStatus.Planned),
                        new VisitorEntity("Anna Schmidt", "Digital Innovations", DateTime.UtcNow, VisitorStatus.Planned),
                        new VisitorEntity("Peter Weber", "Global Consulting", DateTime.UtcNow, VisitorStatus.Planned),
                        
                        // Currently visiting - arrived but not left
                        new VisitorEntity("John Doe", "Acme Corp", DateTime.UtcNow, VisitorStatus.Arrived),
                        new VisitorEntity("Jane Smith", "TechCo", DateTime.UtcNow, VisitorStatus.Arrived),
                        new VisitorEntity("Robert Johnson", "Business Partners Inc", DateTime.UtcNow, VisitorStatus.Arrived)
                    };

                    // Currently visiting - arrived but not left
                    var here1 = new VisitorEntity("John Doe", "Acme Corp", DateTime.UtcNow, VisitorStatus.Arrived);
                    here1.SetArrivedAt(today.AddHours(8));
                    visitors.Add(here1);
                    var here2 = new VisitorEntity("Jane Smith", "TechCo", DateTime.UtcNow, VisitorStatus.Arrived);
                    here2.SetArrivedAt(today.AddHours(8));
                    visitors.Add(here2);
                    var here3 = new VisitorEntity("Robert Johnson", "Business Partners Inc", DateTime.UtcNow, VisitorStatus.Arrived);
                    here3.SetArrivedAt(today.AddHours(8));
                    visitors.Add(here3);

                    // Add some visitors who have already left
                    var leftVisitor1 = new VisitorEntity("Michael Brown", "Software AG", DateTime.UtcNow, VisitorStatus.Arrived);
                    leftVisitor1.SetArrivedAt(today.AddHours(8));
                    leftVisitor1.Leave();
                    leftVisitor1.SetLeftAt(today.AddHours(10));
                    visitors.Add(leftVisitor1);

                    var leftVisitor2 = new VisitorEntity("Sarah Davis", "Marketing Pro", DateTime.UtcNow, VisitorStatus.Arrived);
                    leftVisitor2.SetArrivedAt(today.AddHours(9));
                    leftVisitor2.Leave();
                    leftVisitor2.SetLeftAt(today.AddHours(11));
                    visitors.Add(leftVisitor2);

                    var leftVisitor3 = new VisitorEntity("David Wilson", "Finance Corp", DateTime.UtcNow, VisitorStatus.Arrived);
                    leftVisitor3.SetArrivedAt(today.AddHours(7));
                    leftVisitor3.Leave();
                    leftVisitor3.SetLeftAt(today.AddHours(9));
                    visitors.Add(leftVisitor3);

                    context.Visitors.AddRange(visitors);
                    await context.SaveChangesAsync();
                }
            }
        }
        catch (SqliteException ex)
        {
            // Table might not exist yet during migration - this is expected
            logger?.LogWarning(ex, "Could not seed database, table may not exist yet");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Unexpected error while seeding database");
        }
    }
}


