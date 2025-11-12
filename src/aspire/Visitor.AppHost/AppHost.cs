var builder = DistributedApplication.CreateBuilder(args);

var web = builder.AddProject<Projects.Visitor_Web>("visitor-web")
    .WithEnvironment("ClientUrlHttps", "https://localhost:7138")
    .WithEnvironment("ClientUrlHttp", "http://localhost:5027");

//var client = builder.AddProject<Projects.Visitor_Kiosk>("visitor-kiosk")
//    .WaitFor(web)
//    .WithEnvironment("ServerUrlHttps", web.GetEndpoint("https"))
//    .WithEnvironment("ServerUrlHttp", web.GetEndpoint("http"));

//var client = builder.AddProject<Projects.Visitor_Kiosk>("visitor-kiosk")
//    .WaitFor(web)
//        .WithEnvironment("ServerUrlHttps", web.GetEndpoint("https"))
//    .WithEnvironment("ServerUrlHttp", web.GetEndpoint("http"));

builder.AddProject<Projects.Visitor_Kiosk_Web>("visitor-kiosk-web")
    .WaitFor(web);

//builder.AddProject<Projects.Visitor_Kiosk_Client>("visitor-kiosk-client");

builder.Build().Run();
