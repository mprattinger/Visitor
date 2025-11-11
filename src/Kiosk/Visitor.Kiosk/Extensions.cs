using Microsoft.Extensions.DependencyInjection;

namespace Visitor.Kiosk;

public static class Extensions
{
    public static IServiceCollection? AddKiosk(this IServiceCollection? builder)
    {
        return builder;
    }

}
