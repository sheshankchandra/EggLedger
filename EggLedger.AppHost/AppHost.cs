var builder = DistributedApplication.CreateBuilder(args);

// Add the Postgres database
var username = builder.AddParameter("postgres-username", "eggledger");
var password = builder.AddParameter("postgres-password", "eggledger123", secret: true);

var postgres = builder.AddPostgres("postgres-server", username, password, 5432)
    .WithImage("postgres:15-alpine")
    .WithPgAdmin(pgAdmin => pgAdmin.WithHostPort(5050));
// Uncomment the following line to use a custom Postgres image
//.WithVolume("eggledger_aspire_db", "/var/lib/postgresql/data");

var database = postgres.AddDatabase("eggledgerDB");

// Add the API project (it will use its own configuration)
var api = builder.AddProject<Projects.EggLedger_API>("eggledger-api").WithReference(database).WaitFor(database);

// Add the Vue.js (Vite) frontend with API reference
builder.AddViteApp("eggledger-client", "../EggLedger.Client")
    .WithNpm(installCommand: "ci")
    .WithReference(api)
    .WaitFor(api)
    // Expose the API's dev URL to Vite so the SPA (and the OAuth start URL) target
    // the API directly. Same-site localhost + the environment-aware cookie let the
    // refresh cookie flow over HTTP in development.
    .WithEnvironment("VITE_API_BASE_URL", api.GetEndpoint("http"))
    .WithExternalHttpEndpoints();

builder.Build().Run();
