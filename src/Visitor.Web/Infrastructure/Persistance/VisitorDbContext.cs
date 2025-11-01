using System;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Visitor.Web.Infrastructure.Persistance;

public class VisitorDbContext(DbContextOptions<VisitorDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(modelBuilder);
    }
}
