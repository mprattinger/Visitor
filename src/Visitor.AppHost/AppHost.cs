var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Visitor_Web>("visitor-web");

builder.AddProject<Projects.Visitor_Kiosk>("visitor-kiosk");

builder.Build().Run();
