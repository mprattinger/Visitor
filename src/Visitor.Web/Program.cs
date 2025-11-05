

using Serilog;
using Visitor.Web.Common.Layout;
using Visitor.Web.Features;
using Visitor.Web.Infrastructure;
using Visitor.Web.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

builder.Services.AddSerilog();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.AddInfrastructure();
builder.AddFeatures();

var app = builder.Build();

// Initialize and seed database
try
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<VisitorDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        // Apply migrations first
        await dbContext.Database.MigrateAsync();
        
        // Seed data immediately after migration in the same scope
        await DbSeeder.SeedAsync(dbContext, logger);
    }
}
catch (Exception ex)
{
    Log.Error(ex, "An error occurred while migrating or seeding the database.");
    throw;
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found");
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
