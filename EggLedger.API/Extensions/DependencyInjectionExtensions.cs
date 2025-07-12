using Microsoft.Extensions.DependencyInjection;
using EggLedger.Services.Interfaces;
using EggLedger.Services.Services;
using EggLedger.API.Helpers;

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

        // Background Services
        services.AddHostedService<DatabaseStartupValidationService>();

        return services;
    }
}