using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EggLedger.Models.Options;

namespace EggLedger.API.Extensions;

public static class CorsExtensions
{
    public static IServiceCollection AddApplicationCors(this IServiceCollection services, IConfiguration configuration)
    {
        var corsSection = configuration.GetSection(CorsOptions.SectionName);
        var corsOptions = corsSection.Get<CorsOptions>() ?? new CorsOptions();

        Console.WriteLine($"CORS Policy '{corsOptions.PolicyName}' configured for origins: {string.Join(", ", corsOptions.AllowedOrigins)}");

        services.AddCors(options =>
        {
            options.AddPolicy(name: corsOptions.PolicyName,
                policy =>
                {
                    policy.WithOrigins(corsOptions.AllowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
        });

        return services;
    }

    public static string GetCorsPolicyName(this IConfiguration configuration)
    {
        var corsSection = configuration.GetSection(CorsOptions.SectionName);
        var corsOptions = corsSection.Get<CorsOptions>() ?? new CorsOptions();
        return corsOptions.PolicyName;
    }
}