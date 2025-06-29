using EggLedger.API.Data;
using Microsoft.EntityFrameworkCore;

namespace EggLedger.API.Services
{
    public interface IDatabaseService
    {
        Task<bool> IsAvailableAsync();
        Task<bool> CanConnectAsync();
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

        public async Task<bool> IsAvailableAsync()
        {
            try
            {
                return await _context.Database.CanConnectAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check database availability");
                return false;
            }
        }

        public async Task<bool> CanConnectAsync()
        {
            try
            {
                await _context.Database.OpenConnectionAsync();
                await _context.Database.CloseConnectionAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to database: {Message}", ex.Message);
                return false;
            }
        }
    }
}
