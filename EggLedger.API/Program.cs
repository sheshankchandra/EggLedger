using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using EggLedger.API.Extensions;
using EggLedger.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// Log environment information early
Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"Application Name: {builder.Environment.ApplicationName}");
Console.WriteLine($"Content Root: {builder.Environment.ContentRootPath}");

// Configure services
builder.AddApplicationLogging()
       .AddServiceDefaults();

// Add services to the container
builder.Services.AddApplicationOptions(builder.Configuration);
builder.Services.AddApplicationDatabase(builder.Configuration);
builder.Services.AddApplicationCors(builder.Configuration);
builder.Services.AddApplicationAuthentication(builder.Configuration);
builder.Services.AddApplicationServices();

// Add framework services
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi("v2");

// Build the application
var app = builder.Build();

// Configure middleware pipeline and startup tasks
await app.LogConfigurationSources()
         .HandleDatabaseMigrationAsync();

app.MapDefaultEndpoints()
   .ConfigureApplicationMiddleware();

Console.WriteLine($"EggLedger API starting in '{app.Environment.EnvironmentName}' environment...");
app.Run();