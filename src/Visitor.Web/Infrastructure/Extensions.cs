using Visitor.Web.Infrastructure.Communication;
using Visitor.Web.Infrastructure.Persistance;

namespace Visitor.Web.Infrastructure;

public static class Extensions
{
    public static IHostApplicationBuilder AddInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.AddPersistance();
        builder.AddCommunication();

        return builder;
    }
}
