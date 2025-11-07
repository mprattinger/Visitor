var builder = DistributedApplication.CreateBuilder(args);

var web = builder.AddProject<Projects.Visitor_Web>("visitor-web");

var client = builder.AddProject<Projects.Visitor_Kiosk>("visitor-kiosk")
    .WaitFor(web)
    .WithEnvironment("ServerUrlHttps", web.GetEndpoint("https"))
    .WithEnvironment("ServerUrlHttp", web.GetEndpoint("http"));

builder.Build().Run();
