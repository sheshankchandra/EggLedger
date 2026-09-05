using EggLedger.Services.Interfaces;
using EggLedger.Services.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EggLedger.API.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Core Services
        services.AddScoped<IDatabaseService, DatabaseService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IContainerService, ContainerService>();
        services.AddScoped<IHelperService, HelperService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<ILedgerService, LedgerService>();
        services.AddScoped<IStatsService, StatsService>();
        services.AddScoped<IActivityService, ActivityService>();

        // Background Services
        services.AddHostedService<DatabaseStartupValidationService>();

        return services;
    }
}
