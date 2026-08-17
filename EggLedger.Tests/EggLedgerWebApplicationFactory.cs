using EggLedger.Data;
using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace EggLedger.Tests;

/// <summary>
/// Boots the real API in-process (all middleware, DI, auth, rate limiting) against a
/// throwaway PostgreSQL container, so integration tests exercise the production pipeline.
/// </summary>
public class EggLedgerWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .WithDatabase("eggledger_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    // Functional tests raise the auth limit so their handful of auth calls never trip
    // the throttle. The rate-limiting test overrides this to a low number.
    protected virtual int AuthPermitLimit => 1000;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _db.GetConnectionString(),
                ["Jwt:SecretKey"] = "integration-test-signing-key-that-is-long-enough-1234567890",
                ["Jwt:Issuer"] = "EggLedgerAPI",
                ["Jwt:Audience"] = "EggLedgerAudience",
                ["Jwt:ExpiryInMinutes"] = "15",
                ["Authentication:Google:ClientId"] = "test-client-id",
                ["Authentication:Google:ClientSecret"] = "test-client-secret",
                ["Cors:AllowedOrigins:0"] = "http://localhost:5173",
                ["Cors:PolicyName"] = "_myAllowSpecificOrigins",
                // Tests apply migrations explicitly; keep startup migration off.
                ["Ef_Migrate"] = "false",
                ["RateLimiting:Auth:PermitLimit"] = AuthPermitLimit.ToString(CultureInfo.InvariantCulture),
            });
        });

        // Dev User Secrets override ConfigureAppConfiguration for the connection string,
        // so repoint the DbContext at the container after the app has registered it.
        builder.ConfigureTestServices(services =>
        {
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(ApplicationDbContext)).ToList();
            foreach (var descriptor in toRemove)
                services.Remove(descriptor);

            services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(_db.GetConnectionString()));
        });
    }

    public async Task InitializeAsync()
    {
        await _db.StartAsync();

        // Build the host (triggers config above) and migrate the fresh database.
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _db.DisposeAsync();
        await base.DisposeAsync();
    }
}

/// <summary>Same host, but with the real (low) auth rate limit, for the throttling test.</summary>
public sealed class RateLimitedWebApplicationFactory : EggLedgerWebApplicationFactory
{
    protected override int AuthPermitLimit => 3;
}
