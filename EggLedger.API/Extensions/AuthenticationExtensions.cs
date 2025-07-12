using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using EggLedger.Models.Options;
using EggLedger.API.Helpers.Auth.Handlers;
using EggLedger.API.Helpers.Auth.Requirements;

namespace EggLedger.API.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddApplicationAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // Get JWT settings from configuration
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        var jwtOptions = jwtSection.Get<JwtOptions>() ?? throw new InvalidOperationException("JWT configuration is missing");

        // Get Google Auth settings from configuration
        var googleAuthSection = configuration.GetSection(GoogleAuthOptions.SectionName);
        var googleAuthOptions = googleAuthSection.Get<GoogleAuthOptions>() ?? throw new InvalidOperationException("Google Auth configuration is missing");

        Console.WriteLine($"JWT configured - Issuer: {jwtOptions.Issuer}, Audience: {jwtOptions.Audience}, ExpiryInMinutes: {jwtOptions.ExpiryInMinutes}");

        services.AddAuthentication(options => 
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddJwtBearer(options => 
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.SecretKey)
                    )
                };
            })
            .AddGoogle(options =>
            {
                options.ClientId = googleAuthOptions.ClientId;
                options.ClientSecret = googleAuthOptions.ClientSecret;
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("RoomAdmin", policy => policy.Requirements.Add(new RoomAdminRequirement()));
            options.AddPolicy("RoomMember", policy => policy.Requirements.Add(new RoomMemberRequirement()));
        });

        // Authorization Handlers
        services.AddScoped<IAuthorizationHandler, RoomAdminHandler>();
        services.AddScoped<IAuthorizationHandler, RoomMemberHandler>();

        return services;
    }
}