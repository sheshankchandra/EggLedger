using EggLedger.API.Data;
using EggLedger.Core.DTOs.Container;
using EggLedger.Core.Interfaces;
using EggLedger.Core.Models;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace EggLedger.API.Services
{
    public class ContainerService : IContainerService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ContainerService> _logger;

        public ContainerService(ApplicationDbContext context, ILogger<ContainerService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all containers with summary information.
        /// </summary>
        public async Task<Result<List<ContainerSummaryDto>>> GetAllContainersAsync()
        {
            var containersList = await _context.Containers
                .AsNoTracking()
                .OrderBy(c => c.PurchaseDateTime)
                .Select(c => new ContainerSummaryDto
                {
                    ContainerId = c.ContainerId,
                    ContainerName = c.ContainerName,
                    RemainingQuantity = c.RemainingQuantity,
                    TotalQuantity = c.TotalQuantity,
                    OwnerName = ((c.Buyer.FirstName ?? "").Trim() + " " + (c.Buyer.LastName ?? "").Trim()).Trim()
                })
                .ToListAsync();

            _logger.LogInformation("Found {Count} containers.", containersList.Count);
            return Result.Ok(containersList);
        }

        /// <summary>
        /// Retrieves a single container by its ID with summary information.
        /// </summary>
        public async Task<Result<ContainerSummaryDto>> GetContainerAsync(Guid containerId)
        {
            var container = await _context.Containers
                .AsNoTracking()
                .Where(c => c.ContainerId == containerId)
                .Select(c => new ContainerSummaryDto
                {
                    ContainerId = c.ContainerId,
                    ContainerName = c.ContainerName,
                    RemainingQuantity = c.RemainingQuantity,
                    TotalQuantity = c.TotalQuantity,
                    OwnerName = ((c.Buyer.FirstName ?? "").Trim() + " " + (c.Buyer.LastName ?? "").Trim()).Trim()
                })
                .FirstOrDefaultAsync();

            if (container == null)
            {
                _logger.LogWarning("Container with ID {ContainerId} not found.", containerId);
                return Result.Fail("Container not found");
            }

            _logger.LogInformation("Container {ContainerId} retrieved successfully.", containerId);
            return Result.Ok(container);
        }
    }
}
