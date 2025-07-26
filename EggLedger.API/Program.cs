using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using EggLedger.API.Extensions;
using EggLedger.ServiceDefaults;
using Microsoft.OpenApi.Models;

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

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v2", new OpenApiInfo { Title = "EggLedger API", Version = "v2" });

    // Add JWT Bearer authorization to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Build the application
var app = builder.Build();

// Enable Swagger middleware
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v2/swagger.json", "EggLedger API v2");
    options.RoutePrefix = "swagger";
});

// Configure middleware pipeline and startup tasks
await app.LogConfigurationSources()
         .HandleDatabaseMigrationAsync();

app.MapDefaultEndpoints()
   .ConfigureApplicationMiddleware();

Console.WriteLine($"EggLedger API starting in '{app.Environment.EnvironmentName}' environment...");
app.Run();