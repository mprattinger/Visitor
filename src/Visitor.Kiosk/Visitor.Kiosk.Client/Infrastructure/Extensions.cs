using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Visitor.Kiosk.Client.Infrastructure.Communication;

namespace Visitor.Kiosk.Client.Infrastructure;

public static class Extensions
{
    public static WebAssemblyHostBuilder AddInfrastructure(this WebAssemblyHostBuilder builder)
    {
        builder.AddCommunication();

        return builder;
    }
}
