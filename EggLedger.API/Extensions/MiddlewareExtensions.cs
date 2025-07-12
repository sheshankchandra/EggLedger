using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using EggLedger.API.Middleware;
using EggLedger.Data;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

namespace EggLedger.API.Extensions;

public static class MiddlewareExtensions
{
    public static WebApplication ConfigureApplicationMiddleware(this WebApplication app)
    {
        // Global exception handling (must be first)
        app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

        // Development tools
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        // Health check endpoints
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        // Standard middleware pipeline
        app.UseHttpsRedirection();
        app.UseCors(app.Configuration.GetCorsPolicyName());
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }

    public static async Task<WebApplication> HandleDatabaseMigrationAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
        
        var doEfMigration = app.Configuration["Ef_Migrate"];
        if (doEfMigration == "true")
        {
            try
            {
                logger.LogInformation("Starting database migration process...");
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                // Check if database can be connected to
                var canConnect = await dbContext.Database.CanConnectAsync();
                if (!canConnect)
                {
                    logger.LogError("Cannot connect to database. Migration aborted.");
                    throw new InvalidOperationException("Database connection failed during migration attempt.");
                }

                // Get pending migrations
                var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
                var pendingMigrationsList = pendingMigrations.ToList();
                
                if (pendingMigrationsList.Count > 0)
                {
                    logger.LogInformation("Found {Count} pending migrations: {Migrations}", 
                        pendingMigrationsList.Count, 
                        string.Join(", ", pendingMigrationsList));
                    
                    // Apply migrations with timeout
                    var migrationTask = dbContext.Database.MigrateAsync();
                    var timeoutTask = Task.Delay(TimeSpan.FromMinutes(5)); // 5-minute timeout
                    
                    var completedTask = await Task.WhenAny(migrationTask, timeoutTask);
                    if (completedTask == timeoutTask)
                    {
                        logger.LogError("Database migration timed out after 5 minutes");
                        throw new TimeoutException("Database migration operation timed out");
                    }
                    
                    await migrationTask; // Ensure any exceptions are thrown
                    logger.LogInformation("Database migration completed successfully");
                }
                else
                {
                    logger.LogInformation("Database is up to date. No migrations needed.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Database migration failed: {Message}", ex.Message);
                
                // In production, you might want to fail fast rather than continue with a potentially inconsistent database
                if (app.Environment.IsProduction())
                {
                    logger.LogCritical("Application will not start due to migration failure in production environment");
                    throw;
                }
                
                logger.LogWarning("Migration failed in non-production environment. Application will continue startup.");
            }
        }
        else
        {
            logger.LogInformation("Database migration is disabled (Ef_Migrate != 'true')");
        }

        return app;
    }

    public static WebApplication LogConfigurationSources(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        
        Console.WriteLine($"Configuration sources loaded for environment '{app.Environment.EnvironmentName}':");
        if (configuration is IConfigurationRoot configRoot)
        {
            foreach (var provider in configRoot.Providers)
            {
                Console.WriteLine($"  - {provider.GetType().Name}");
            }
        }

        return app;
    }
}