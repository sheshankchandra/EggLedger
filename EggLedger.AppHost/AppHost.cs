var builder = DistributedApplication.CreateBuilder(args);

// Add the Postgres database
var username = builder.AddParameter("postgres-username", "eggledger");
var password = builder.AddParameter("postgres-password", "eggledger123", secret: true);

var postgres = builder.AddPostgres("postgres-server", username, password, 5432)
    .WithImage("postgres:15-alpine");
    // Uncomment the following line to use a custom Postgres image
    //.WithVolume("eggledger_aspire_db", "/var/lib/postgresql/data");

var database = postgres.AddDatabase("eggledgerDB");

// Add the API project (it will use its own configuration)
var api = builder.AddProject<Projects.EggLedger_API>("eggledger-api").WithReference(database).WaitFor(database);

// Add the Vue.js frontend with API reference
builder.AddNpmApp("eggledger-client", "../EggLedger.Client")
    .WithReference(api)
    .WaitFor(api)
    .WithUrl("http://localhost:5173")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

builder.Build().Run();
