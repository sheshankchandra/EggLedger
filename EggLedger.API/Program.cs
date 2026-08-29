using System;
using EggLedger.API.Extensions;
using EggLedger.ServiceDefaults;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Log environment information early
Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"Application Name: {builder.Environment.ApplicationName}");
Console.WriteLine($"Content Root: {builder.Environment.ContentRootPath}");

// Configure services
builder.AddApplicationLogging()
       .AddServiceDefaults();

// Honor X-Forwarded-* from the Container Apps ingress so the app sees the original
// https scheme and client host. Required for correct OAuth redirect URIs and cookie
// behavior behind the proxy. The ingress hop is trusted and its IP is dynamic, so the
// known-networks/proxies allow-lists are cleared.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add services to the container
builder.Services.AddApplicationOptions(builder.Configuration);
builder.Services.AddApplicationDatabase(builder.Configuration);
builder.Services.AddApplicationCors(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddApplicationAuthentication(builder.Configuration);
builder.Services.AddApplicationRateLimiting();
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

// Exposed so the integration test project can drive the app via WebApplicationFactory<Program>.
public partial class Program;
