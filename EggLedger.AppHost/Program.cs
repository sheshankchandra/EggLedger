var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.EggLedger_API>("eggledger-api");

builder.Build().Run();
