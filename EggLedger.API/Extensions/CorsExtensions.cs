using System;
using EggLedger.Models.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EggLedger.API.Extensions;

public static class CorsExtensions
{
    public static IServiceCollection AddApplicationCors(this IServiceCollection services, IConfiguration configuration, bool isDevelopment = false)
    {
        var corsSection = configuration.GetSection(CorsOptions.SectionName);
        var corsOptions = corsSection.Get<CorsOptions>() ?? new CorsOptions();

        Console.WriteLine($"CORS Policy '{corsOptions.PolicyName}' configured for origins: {string.Join(", ", corsOptions.AllowedOrigins)}");

        services.AddCors(options =>
        {
            options.AddPolicy(name: corsOptions.PolicyName,
                policy =>
                {
                    if (isDevelopment)
                    {
                        // The Aspire dev server binds to a dynamic port, so allow any
                        // localhost origin during local development.
                        policy.SetIsOriginAllowed(IsLocalhostOrigin)
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    }
                    else
                    {
                        policy.WithOrigins(corsOptions.AllowedOrigins)
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    }
                });
        });

        return services;
    }

    public static bool IsLocalhostOrigin(string? origin)
    {
        return !string.IsNullOrEmpty(origin) &&
               Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
               (uri.Host == "localhost" || uri.Host == "127.0.0.1");
    }

    public static bool IsAllowedOrigin(string? origin, CorsOptions corsOptions, bool isDevelopment)
    {
        if (string.IsNullOrEmpty(origin))
        {
            return false;
        }

        if (isDevelopment && IsLocalhostOrigin(origin))
        {
            return true;
        }

        return corsOptions.AllowedOrigins.Contains(origin);
    }

    public static string GetCorsPolicyName(this IConfiguration configuration)
    {
        var corsSection = configuration.GetSection(CorsOptions.SectionName);
        var corsOptions = corsSection.Get<CorsOptions>() ?? new CorsOptions();
        return corsOptions.PolicyName;
    }
}