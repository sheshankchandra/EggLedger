var builder = DistributedApplication.CreateBuilder(args);

// Add the API project (it will use its own configuration)
var api = builder.AddProject<Projects.EggLedger_API>("eggledger-api");

// Add the Vue.js frontend with API reference
builder.AddNpmApp("eggledger-client", "../EggLedger.Client", "dev")
    .WithReference(api)
    .WithHttpEndpoint(env: "VITE_PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

builder.Build().Run();
