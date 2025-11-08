using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Visitor.Web.Common.Domains;

namespace Visitor.Web.Infrastructure.Persistance;

public class VisitorDbContext(DbContextOptions<VisitorDbContext> options) : DbContext(options)
{
    public DbSet<VisitorEntity> Visitors { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(modelBuilder);
    }
}
