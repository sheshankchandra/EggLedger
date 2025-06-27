using EggLedger.API.Data;
using EggLedger.Core.DTOs.Container;
using EggLedger.Core.Interfaces;
using EggLedger.Core.Models;
using FluentResults;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Diagnostics;
using Container = EggLedger.Core.Models.Container;

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
                    PurchaseDateTime = c.PurchaseDateTime,
                    BuyerName = c.Buyer.Name,
                    TotalQuantity = c.TotalQuantity,
                    RemainingQuantity = c.RemainingQuantity,
                    Amount = c.Amount,
                    RoomName = c.Room.RoomName,
                    Price = c.Price,
                    CompletedDateTime = c.CompletedDateTime
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
                .Include(container => container.Buyer)
                .Include(container => container.Room)
                .FirstOrDefaultAsync(c => c.ContainerId == containerId);

            if (container == null)
            {
                _logger.LogWarning("Container with ID {ContainerId} not found.", containerId);
                return Result.Fail("Container not found");
            }

            ContainerSummaryDto summaryDto = new ContainerSummaryDto
            {
                ContainerId = container.ContainerId,
                ContainerName = container.ContainerName,
                PurchaseDateTime = container.PurchaseDateTime,
                BuyerName = container.Buyer.Name,
                TotalQuantity = container.TotalQuantity,
                RemainingQuantity = container.RemainingQuantity,
                Amount = container.Amount,
                RoomName = container.Room.RoomName,
                Price = container.Price,
                CompletedDateTime = container.CompletedDateTime
            };

            _logger.LogInformation("Container {ContainerName} retrieved successfully.", summaryDto.ContainerName);
            return Result.Ok(summaryDto);
        }

        public async Task<Result<ContainerSummaryDto>> UpdateContainerAsync(Guid containerId, ContainerUpdateDto dto)
        {
            var container = await _context.Containers
                .Include(container => container.Room)
                .Include(container => container.Buyer)
                .FirstOrDefaultAsync(c => c.ContainerId == containerId);

            if (container == null)
            {
                _logger.LogWarning("Container with ID {ContainerId} not found.", containerId);
                return Result.Fail("Container not found");
            }

            // Update only provided properties
            container.ContainerName = dto.ContainerName ?? container.ContainerName;
            container.PurchaseDateTime = dto.PurchaseDateTime ?? container.PurchaseDateTime;
            container.TotalQuantity = dto.TotalQuantity ?? container.TotalQuantity;
            container.RemainingQuantity = dto.RemainingQuantity ?? container.RemainingQuantity;
            container.Amount = dto.Amount ?? container.Amount;

            await _context.SaveChangesAsync();

            var buyer = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == container.BuyerId);

            var containerDto = new ContainerSummaryDto
            {
                ContainerId = container.ContainerId,
                ContainerName = container.ContainerName,
                PurchaseDateTime = container.PurchaseDateTime,
                BuyerName = container.Buyer.Name,
                TotalQuantity = container.TotalQuantity,
                RemainingQuantity = container.RemainingQuantity,
                Amount = container.Amount,
                RoomName = container.Room.RoomName,
                Price = container.Price,
                CompletedDateTime = container.CompletedDateTime
            };

            _logger.LogInformation("Container {ContainerId} updated successfully.", container.ContainerId);
            return Result.Ok(containerDto);
        }

        public async Task<Result> DeleteContainerAsync(Guid containerId)
        {
            var container = await _context.Containers
                .FirstOrDefaultAsync(c => c.ContainerId == containerId);

            if (container == null)
            {
                _logger.LogWarning("Container with ID {ContainerId} not found.", containerId);
                return Result.Fail("Container not found");
            }

            _context.Containers.Remove(container);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Container {ContainerId} deleted successfully.", containerId);
            return Result.Ok();
        }

        public async Task<Result<List<ContainerSummaryDto>>> SearchContainersByOwnerNameAsync(string ownerName)
        {
            if (string.IsNullOrWhiteSpace(ownerName))
            {
                return await GetAllContainersAsync();
            }

            var containers = await _context.Containers
                .AsNoTracking()
                .Where(c => (c.Buyer.FirstName + " " + c.Buyer.LastName).Contains(ownerName))
                .OrderBy(c => c.PurchaseDateTime)
                .Select(container => new ContainerSummaryDto
                {
                    ContainerId = container.ContainerId,
                    ContainerName = container.ContainerName,
                    PurchaseDateTime = container.PurchaseDateTime,
                    BuyerName = container.Buyer.Name,
                    TotalQuantity = container.TotalQuantity,
                    RemainingQuantity = container.RemainingQuantity,
                    Amount = container.Amount,
                    RoomName = container.Room.RoomName,
                    Price = container.Price,
                    CompletedDateTime = container.CompletedDateTime
                })
                .ToListAsync();

            return Result.Ok(containers);
        }

        public async Task<Result<List<ContainerSummaryDto>>> GetPagedContainersAsync(int page, int pageSize)
        {
            // Validate pagination parameters
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100); // Limit page size to 100

            var containers = await _context.Containers
                .AsNoTracking()
                .OrderBy(c => c.PurchaseDateTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(container => new ContainerSummaryDto
                {
                    ContainerId = container.ContainerId,
                    ContainerName = container.ContainerName,
                    PurchaseDateTime = container.PurchaseDateTime,
                    BuyerName = container.Buyer.Name,
                    TotalQuantity = container.TotalQuantity,
                    RemainingQuantity = container.RemainingQuantity,
                    Amount = container.Amount,
                    RoomName = container.Room.RoomName,
                    Price = container.Price,
                    CompletedDateTime = container.CompletedDateTime
                })
                .ToListAsync();

            return Result.Ok(containers);
        }
    }
}
