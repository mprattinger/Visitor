using Microsoft.EntityFrameworkCore;
using Visitor.Web.Infrastructure.Persistance;

namespace Visitor.Web.Infrastructure;

public static class Extensions
{
    public static IHostApplicationBuilder AddInfrastructure(this IHostApplicationBuilder builder)
    {
        // Add DbContext
        builder.Services.AddDbContext<VisitorDbContext>(options =>
            options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

        return builder;
    }
}
