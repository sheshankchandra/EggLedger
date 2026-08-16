using System;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
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
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });

            // Stricter budget for auth endpoints (login/register/refresh/logout)
            // to slow credential stuffing and token-refresh abuse.
            options.AddPolicy(AuthPolicy, context =>
            {
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });
        });

        return services;
    }
}
