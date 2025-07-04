using EggLedger.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EggLedger.Services.Services
{
    public interface IDatabaseService
    {
        Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
        Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);
    }

    public class DatabaseService : IDatabaseService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DatabaseService> _logger;

        public DatabaseService(ApplicationDbContext context, ILogger<DatabaseService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.Database.CanConnectAsync(cancellationToken);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "Database availability check was canceled");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check database availability");
                return false;
            }
        }

        public async Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _context.Database.OpenConnectionAsync(cancellationToken);
                await _context.Database.CloseConnectionAsync();
                return true;
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "Database connection check was canceled");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to database: {Message}", ex.Message);
                return false;
            }
        }
    }
}
