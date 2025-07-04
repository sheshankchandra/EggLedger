using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EggLedger.Services.Services
{
    public class DatabaseStartupValidationService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DatabaseStartupValidationService> _logger;

        public DatabaseStartupValidationService(IServiceProvider serviceProvider, ILogger<DatabaseStartupValidationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Validating database connection on startup...");

            using var scope = _serviceProvider.CreateScope();
            var databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();

            try
            {
                var isAvailable = await databaseService.IsAvailableAsync();
                
                if (isAvailable)
                {
                    _logger.LogInformation("✅ Database connection validated successfully");
                }
                else
                {
                    _logger.LogWarning("⚠️ Database is not available on startup. The application will continue, but database-dependent features may fail.");
                    _logger.LogWarning("Please ensure PostgreSQL is running and the connection string is correct.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to validate database connection on startup: {Message}", ex.Message);
                _logger.LogWarning("The application will continue, but database-dependent features will fail until the database is available.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
