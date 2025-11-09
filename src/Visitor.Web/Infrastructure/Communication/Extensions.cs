using Visitor.Web.Common.Interfaces;
using Visitor.Web.Infrastructure.Communication.Services;

namespace Visitor.Web.Infrastructure.Communication;

public static class Extensions
{
    public static IHostApplicationBuilder AddCommunication(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSignalR();

        builder.Services.AddScoped<IVisitCommunicationService, VisitCommunicationService>();
        builder.Services.AddSingleton<IVisitorUpdateNotifier, VisitorUpdateNotifier>();

        return builder;
    }

    public static IEndpointRouteBuilder? UseCommunication(this IEndpointRouteBuilder? app)
    {
        app?.MapHub<VisitHub>("/visithub");

        return app;
    }
}
