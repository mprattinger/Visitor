using Visitor.Web.Features.VisitorManagement.DomainEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

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
                        new VisitorEntity("Max Mustermann", "Tech Solutions GmbH", VisitorStatus.Planned),
                        new VisitorEntity("Anna Schmidt", "Digital Innovations", VisitorStatus.Planned),
                        new VisitorEntity("Peter Weber", "Global Consulting", VisitorStatus.Planned),
                        
                        // Currently visiting - arrived but not left
                        VisitorEntity.CreateVisitorFromKiosk("John Doe", "Acme Corp"),
                        VisitorEntity.CreateVisitorFromKiosk("Jane Smith", "TechCo"),
                        VisitorEntity.CreateVisitorFromKiosk("Robert Johnson", "Business Partners Inc"),
                    };

                    // Add some visitors who have already left
                    var leftVisitor1 = VisitorEntity.CreateVisitorFromKiosk("Michael Brown", "Software AG");
                    leftVisitor1.SetArrivedAt(today.AddHours(8));
                    leftVisitor1.Leave();
                    leftVisitor1.SetLeftAt(today.AddHours(10));
                    visitors.Add(leftVisitor1);

                    var leftVisitor2 = VisitorEntity.CreateVisitorFromKiosk("Sarah Davis", "Marketing Pro");
                    leftVisitor2.SetArrivedAt(today.AddHours(9));
                    leftVisitor2.Leave();
                    leftVisitor2.SetLeftAt(today.AddHours(11));
                    visitors.Add(leftVisitor2);

                    var leftVisitor3 = VisitorEntity.CreateVisitorFromKiosk("David Wilson", "Finance Corp");
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


