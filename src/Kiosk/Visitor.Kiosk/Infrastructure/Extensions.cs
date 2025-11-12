using Microsoft.Extensions.DependencyInjection;
using Visitor.Kiosk.Common.Interfaces;
using Visitor.Kiosk.Infrastructure.Communication;

namespace Visitor.Kiosk.Infrastructure;

public static class Extensions
{
    public static IServiceCollection? AddInfrastructure(this IServiceCollection? services)
    {
        services?.AddScoped<IVisitCommunicationClientService, VisitCommunicationClientService>();

        return services;
    }

}