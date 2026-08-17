using System;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EggLedger.API.Extensions;

public static class RateLimitingExtensions
{
    // Named policy applied to the sensitive auth endpoints.
    public const string AuthPolicy = "auth";

    public static IServiceCollection AddApplicationRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Baseline DoS guard: partition by client IP, generous per-minute budget.
            // NOTE: in-memory limits are per-replica; behind multiple instances the
            // effective limit multiplies. Move to a distributed (Redis) limiter if
            // strict global limits are ever required.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var (permit, window) = ReadLimits(context, "Global", defaultPermit: 100, defaultWindow: 60);
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permit,
                    Window = TimeSpan.FromSeconds(window),
                    QueueLimit = 0
                });
            });

            // Stricter budget for auth endpoints (login/register/refresh/logout)
            // to slow credential stuffing and token-refresh abuse.
            options.AddPolicy(AuthPolicy, context =>
            {
                var (permit, window) = ReadLimits(context, "Auth", defaultPermit: 10, defaultWindow: 60);
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter($"auth:{ip}", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permit,
                    Window = TimeSpan.FromSeconds(window),
                    QueueLimit = 0
                });
            });
        });

        return services;

        // Limits are read from the resolved configuration so they can be tuned per
        // environment (or raised in tests) without recompiling.
        static (int permit, int window) ReadLimits(HttpContext context, string section, int defaultPermit, int defaultWindow)
        {
            var config = context.RequestServices.GetRequiredService<IConfiguration>();
            var permit = config.GetValue<int?>($"RateLimiting:{section}:PermitLimit") ?? defaultPermit;
            var window = config.GetValue<int?>($"RateLimiting:{section}:WindowSeconds") ?? defaultWindow;
            return (permit, window);
        }
    }
}
