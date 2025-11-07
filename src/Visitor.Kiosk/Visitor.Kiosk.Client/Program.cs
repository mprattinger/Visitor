using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Visitor.Kiosk.Client.Infrastructure;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.AddInfrastructure();

await builder.Build().RunAsync();
