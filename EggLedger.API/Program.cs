using EggLedger.API.Data;
using EggLedger.API.Services;
using EggLedger.Core.Helpers;
using EggLedger.Core.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;
using EggLedger.API.Helpers.Auth.Handlers;
using EggLedger.API.Helpers.Auth.Requirements;
using EggLedger.API.Middleware;
using log4net;
using log4net.Config;

var builder = WebApplication.CreateBuilder(args);

#region Logging Configuration
// Configure log4net for file-based logging
var logRepository = LogManager.GetRepository(System.Reflection.Assembly.GetEntryAssembly() ?? System.Reflection.Assembly.GetExecutingAssembly());
XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

// Clear default providers and use log4net
builder.Logging.ClearProviders();
builder.Logging.AddLog4Net();
#endregion

#region Aspire Service Defaults
builder.AddServiceDefaults();
#endregion

#region Basic Web API Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi("v2");
#endregion

#region Database Configuration
// PostgreSQL connection with retry policy and health checks
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Database connection string 'DefaultConnection' is not configured. Please check your appsettings.json or environment variables.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options => 
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
    }));

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgresql");
#endregion

#region CORS Configuration
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
        policy =>
        {
            policy.WithOrigins("http://localhost:5173") // Your Vue app's URL
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});
#endregion

#region Authentication & Authorization
builder.Services.AddAuthentication(options => 
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
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"])
            )
        };
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RoomAdmin", policy => policy.Requirements.Add(new RoomAdminRequirement()));
    options.AddPolicy("RoomMember", policy => policy.Requirements.Add(new RoomMemberRequirement()));
});
#endregion

#region Dependency Injection
builder.Services.AddHttpContextAccessor();

// Core Services
builder.Services.AddScoped<IDatabaseService, DatabaseService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IContainerService, ContainerService>();
builder.Services.AddScoped<IHelperService, HelperService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoomService, RoomService>();

// Background Services
builder.Services.AddHostedService<EggLedger.API.Middleware.DatabaseStartupValidationService>();
builder.Services.AddHostedService<RefreshTokenCleanupService>();

// Authorization Handlers
builder.Services.AddScoped<IAuthorizationHandler, RoomAdminHandler>();
builder.Services.AddScoped<IAuthorizationHandler, RoomMemberHandler>();
#endregion

var app = builder.Build();

#region Aspire Endpoints
app.MapDefaultEndpoints();
#endregion

#region Middleware Pipeline
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
app.UseCors(MyAllowSpecificOrigins);
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
#endregion

app.Run();