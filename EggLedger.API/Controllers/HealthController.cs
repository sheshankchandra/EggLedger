using EggLedger.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace EggLedger.API.Controllers
{
    [ApiController]
    [Route("egg-ledger-api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<HealthController> _logger;

        public HealthController(IDatabaseService databaseService, ILogger<HealthController> logger)
        {
            _databaseService = databaseService;
            _logger = logger;
        }

        // GET: egg-ledger-api/health
        [HttpGet]
        public async Task<IActionResult> GetHealth(CancellationToken cancellationToken)
        {
            try
            {
                var isHealthy = await _databaseService.IsAvailableAsync(cancellationToken);
                
                var health = new
                {
                    status = isHealthy ? "Healthy" : "Unhealthy",
                    timestamp = DateTime.UtcNow,
                    services = new
                    {
                        database = new
                        {
                            status = isHealthy ? "Connected" : "Disconnected",
                            canConnect = isHealthy
                        }
                    }
                };

                if (isHealthy)
                {
                    return Ok(health);
                }
                else
                {
                    return StatusCode(503, health); // Service Unavailable
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Request was canceled by the client for GetHealth");
                return StatusCode(499, "Client closed request.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in GetHealth");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        // GET: egg-ledger-api/health/database
        [HttpGet("database")]
        public async Task<IActionResult> GetDatabaseHealth(CancellationToken cancellationToken)
        {
            try
            {
                var canConnect = await _databaseService.CanConnectAsync(cancellationToken);
                
                var response = new
                {
                    status = canConnect ? "Connected" : "Disconnected",
                    canConnect = canConnect,
                    timestamp = DateTime.UtcNow,
                    message = canConnect 
                        ? "Database is available and accepting connections." 
                        : "Database is not available. Please ensure PostgreSQL is running and configured correctly."
                };

                return canConnect ? Ok(response) : StatusCode(503, response);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Request was canceled by the client for GetDatabaseHealth");
                return StatusCode(499, "Client closed request.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking database health");
                
                var errorResponse = new
                {
                    status = "Error",
                    canConnect = false,
                    timestamp = DateTime.UtcNow,
                    message = "Unable to check database status.",
                    error = ex.Message
                };

                return StatusCode(503, errorResponse);
            }
        }
    }
}
