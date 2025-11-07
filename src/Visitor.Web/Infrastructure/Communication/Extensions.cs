using Visitor.Shared.Common.Interfaces;
using Visitor.Web.Infrastructure.Communication.Services;

namespace Visitor.Web.Infrastructure.Communication;

public static class Extensions
{
    public static IHostApplicationBuilder AddCommunication(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSignalR();
        builder.Services.AddScoped<IVisitCommunicationService, VisitCommunicationService>();

        return builder;
    }
}
