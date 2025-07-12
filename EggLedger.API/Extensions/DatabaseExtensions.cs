using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EggLedger.Data;

namespace EggLedger.API.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddApplicationDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Database connection string 'DefaultConnection' is not configured. Please check your appsettings.json or environment variables.");
        }

        // Log connection string (with masked password)
        var maskedConnectionString = MaskPassword(connectionString);
        Console.WriteLine($"Using database connection: {maskedConnectionString}");

        services.AddDbContext<ApplicationDbContext>(options => 
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            }));

        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "postgresql");

        return services;
    }

    private static string MaskPassword(string connectionString)
    {
        try
        {
            var parts = connectionString.Split(';');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Trim().StartsWith("Password=", StringComparison.OrdinalIgnoreCase))
                {
                    parts[i] = "Password=***";
                }
            }
            return string.Join(";", parts);
        }
        catch
        {
            return "***";
        }
    }
}