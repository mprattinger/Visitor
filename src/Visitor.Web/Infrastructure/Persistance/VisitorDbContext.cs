using System;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Visitor.Web.Features.VisitorManagement.DomainEntities;

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
