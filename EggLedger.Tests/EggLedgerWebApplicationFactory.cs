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
    static EggLedgerWebApplicationFactory()
    {
        // These keys are read during service registration (builder.Configuration),
        // which WebApplicationFactory.ConfigureAppConfiguration does NOT influence.
        // Environment variables ARE read by CreateBuilder, so set them here to make the
        // config available at registration in CI (which has no dev User Secrets). Values
        // are identical for every factory, so sharing the process env is safe; the real,
        // per-factory database connection is swapped in via ConfigureTestServices below.
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection",
            "Host=placeholder;Port=5432;Database=placeholder;Username=placeholder;Password=placeholder");
        Environment.SetEnvironmentVariable("Jwt__SecretKey", "integration-test-signing-key-that-is-long-enough-1234567890");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "EggLedgerAPI");
        Environment.SetEnvironmentVariable("Jwt__Audience", "EggLedgerAudience");
        Environment.SetEnvironmentVariable("Jwt__ExpiryInMinutes", "15");
        Environment.SetEnvironmentVariable("Authentication__Google__ClientId", "test-client-id");
        Environment.SetEnvironmentVariable("Authentication__Google__ClientSecret", "test-client-secret");
        Environment.SetEnvironmentVariable("Cors__AllowedOrigins__0", "http://localhost:5173");
        Environment.SetEnvironmentVariable("Cors__PolicyName", "_myAllowSpecificOrigins");
    }

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
            // Runtime/per-factory values (read from the resolved configuration, not at
            // registration): keep startup migration off and set this factory's auth limit.
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
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
