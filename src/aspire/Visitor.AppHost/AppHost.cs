var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Visitor_Web>("visitor-web");

builder.AddDockerComposeEnvironment("compose");

builder.Build().Run();
