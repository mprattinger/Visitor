using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Visitor.Kiosk;
using Visitor.Kiosk.Common.Interfaces;
using Visitor.Kiosk.Infrastructure.Communication;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<IVisitCommunicationClientService, VisitCommunicationClientService>();

await builder.Build().RunAsync();
