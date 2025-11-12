using Microsoft.Extensions.DependencyInjection;
using Visitor.Kiosk.Infrastructure;

namespace Visitor.Kiosk;

public static class Extensions
{
    public static IServiceCollection? AddKiosk(this IServiceCollection? services)
    {
        services.AddInfrastructure();

        return services;
    }

}
