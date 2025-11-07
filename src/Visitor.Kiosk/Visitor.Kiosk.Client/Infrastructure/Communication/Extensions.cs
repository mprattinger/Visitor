using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Visitor.Kiosk.Client.Infrastructure.Communication.Services;
using Visitor.Shared.Common.Interfaces;

namespace Visitor.Kiosk.Client.Infrastructure.Communication;

public static class Extensions
{
    public static WebAssemblyHostBuilder AddCommunication(this WebAssemblyHostBuilder builder)
    {
        builder.Services.AddScoped<IVisitCommunicationService, VisitCommunicationClientService>();

        return builder;
    }
}
