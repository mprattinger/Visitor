using Microsoft.EntityFrameworkCore;

namespace Visitor.Web.Infrastructure.Persistance;

public static class Extensions
{
    public static IHostApplicationBuilder AddPersistance(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDbContext<VisitorDbContext>(options =>
        {
            options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
        });
        return builder;
    }
}
