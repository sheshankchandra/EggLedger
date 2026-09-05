using EggLedger.Data;
using EggLedger.DTO.Container;
using EggLedger.Models.Enums;
using EggLedger.Models.Models;
using EggLedger.Services.Errors;
using EggLedger.Services.Extensions;
using EggLedger.Services.Interfaces;
using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EggLedger.Services.Services;

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
    /// Retrieves all active containers with summary information for a specific room.
    /// </summary>
    public async Task<Result<List<ContainerSummaryDto>>> GetAllContainersAsync(int roomCode, CancellationToken cancellationToken = default)
    {
        return await _logger.ExecuteAsync(async () =>
        {
            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomCode == roomCode, cancellationToken);
            if (room == null)
                return Result.Fail(new NotFoundError("Room not found"));

            var containersList = await _context.Containers
                .AsNoTracking()
                .Where(c => c.RoomId == room.RoomId && c.Status == ContainerStatus.Available && c.RemainingQuantity > 0)
                .OrderBy(c => c.PurchaseDateTime)
                .Select(c => new ContainerSummaryDto
                {
                    ContainerId = c.ContainerId,
                    ContainerName = c.ContainerName,
                    PurchaseDateTime = c.PurchaseDateTime,
                    BuyerId = c.BuyerId,
                    BuyerName = c.Buyer.Name,
                    TotalQuantity = c.TotalQuantity,
                    RemainingQuantity = c.RemainingQuantity,
                    Amount = c.Amount,
                    RoomName = c.Room.RoomName,
                    Status = c.Status,
                    Price = c.Price,
                    DeletedAt = c.DeletedAt,
                    DeletionReason = c.DeletionReason
                })
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {Count} containers in room {RoomName}.", containersList.Count, room.RoomName);
            return Result.Ok(containersList);
        }, "An error occurred while retrieving containers.");
    }

    /// <summary>
    /// Retrieves a single container by its ID with summary information.
    /// </summary>
    public async Task<Result<ContainerSummaryDto>> GetContainerAsync(Guid containerId, CancellationToken cancellationToken = default)
    {
        return await _logger.ExecuteAsync(async () =>
        {
            var container = await _context.Containers
                .Include(container => container.Buyer)
                .Include(container => container.Room)
                .FirstOrDefaultAsync(c => c.ContainerId == containerId, cancellationToken);

            if (container == null)
            {
                _logger.LogWarning("Container with ID {ContainerId} not found.", containerId);
                return Result.Fail(new NotFoundError("Container not found"));
            }

            var summaryDto = new ContainerSummaryDto
            {
                ContainerId = container.ContainerId,
                ContainerName = container.ContainerName,
                PurchaseDateTime = container.PurchaseDateTime,
                BuyerId = container.BuyerId,
                BuyerName = container.Buyer.Name,
                TotalQuantity = container.TotalQuantity,
                RemainingQuantity = container.RemainingQuantity,
                Amount = container.Amount,
                RoomName = container.Room.RoomName,
                Status = container.Status,
                Price = container.Price,
                DeletedAt = container.DeletedAt,
                DeletionReason = container.DeletionReason
            };

            _logger.LogInformation("Container {ContainerName} retrieved successfully.", summaryDto.ContainerName);
            return Result.Ok(summaryDto);
        }, "An error occurred while retrieving the container.");
    }

    public async Task<Result<ContainerSummaryDto>> UpdateContainerAsync(Guid containerId, ContainerUpdateDto dto, CancellationToken cancellationToken = default)
    {
        return await _logger.ExecuteAsync(async () =>
        {
            var container = await _context.Containers
                .Include(container => container.Room)
                .Include(container => container.Buyer)
                .FirstOrDefaultAsync(c => c.ContainerId == containerId, cancellationToken);

            if (container == null)
            {
                _logger.LogWarning("Container with ID {ContainerId} not found.", containerId);
                return Result.Fail(new NotFoundError("Container not found"));
            }

            // Update only provided properties
            container.ContainerName = dto.ContainerName ?? container.ContainerName;
            container.PurchaseDateTime = dto.PurchaseDateTime ?? container.PurchaseDateTime;
            container.TotalQuantity = dto.TotalQuantity ?? container.TotalQuantity;
            container.RemainingQuantity = dto.RemainingQuantity ?? container.RemainingQuantity;
            container.Amount = dto.Amount ?? container.Amount;

            await _context.SaveChangesAsync(cancellationToken);

            var containerDto = new ContainerSummaryDto
            {
                ContainerId = container.ContainerId,
                ContainerName = container.ContainerName,
                PurchaseDateTime = container.PurchaseDateTime,
                BuyerId = container.BuyerId,
                BuyerName = container.Buyer.Name,
                TotalQuantity = container.TotalQuantity,
                RemainingQuantity = container.RemainingQuantity,
                Amount = container.Amount,
                RoomName = container.Room.RoomName,
                Status = container.Status,
                Price = container.Price,
                DeletedAt = container.DeletedAt,
                DeletionReason = container.DeletionReason
            };

            _logger.LogInformation("Container {ContainerId} updated successfully.", container.ContainerId);
            return Result.Ok(containerDto);
        }, "An error occurred while updating the container.");
    }

    public async Task<Result> ArchiveContainerAsync(Guid containerId, CancellationToken cancellationToken = default)
    {
        return await _logger.ExecuteAsync(async () =>
        {
            var container = await _context.Containers
                .FirstOrDefaultAsync(c => c.ContainerId == containerId, cancellationToken);

            if (container == null)
            {
                _logger.LogWarning("Container with ID {ContainerId} not found.", containerId);
                return Result.Fail(new NotFoundError("Container not found"));
            }

            container.Status = ContainerStatus.Archived;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Container {ContainerId} Archived successfully.", containerId);
            return Result.Ok();
        }, "An error occurred while Archiving the container.");
    }

    public async Task<Result> DeleteContainerAsync(Guid containerId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _logger.ExecuteAsync(async () =>
        {
            var container = await _context.Containers
                .FirstOrDefaultAsync(c => c.ContainerId == containerId && c.Status != ContainerStatus.Archived, cancellationToken);

            if (container == null)
            {
                _logger.LogWarning("Container with ID {ContainerId} not found.", containerId);
                return Result.Fail(new NotFoundError("Container not found"));
            }

            if (container.BuyerId != userId)
            {
                _logger.LogWarning("User {UserId} is not the owner of container {ContainerId}", userId, containerId);
                return Result.Fail(new ForbiddenError("Only the owner can delete this container"));
            }

            var consumed = container.TotalQuantity - container.RemainingQuantity;
            if (consumed > 0)
            {
                _logger.LogWarning("Container {ContainerId} has {Consumed} consumed eggs and cannot be deleted", containerId, consumed);
                return Result.Fail(new ConflictError($"Cannot delete: {consumed} egg(s) have already been consumed from this container."));
            }

            var now = DateTime.UtcNow;
            container.Status = ContainerStatus.Archived;
            container.DeletedAt = now;
            container.DeletedBy = userId;
            container.DeletionReason = "Deleted by owner";
            container.ModifiedAt = now;
            container.ModifiedBy = userId;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Container {ContainerId} deleted by owner {UserId}.", containerId, userId);
            return Result.Ok();
        }, "An error occurred while deleting the container.");
    }

    public async Task<Result> SuspendContainerAsync(Guid containerId, CancellationToken cancellationToken = default)
    {
        return await _logger.ExecuteAsync(async () =>
        {
            var container = await _context.Containers
                .FirstOrDefaultAsync(c => c.ContainerId == containerId, cancellationToken);

            if (container == null)
            {
                _logger.LogWarning("Container with ID {ContainerId} not found.", containerId);
                return Result.Fail(new NotFoundError("Container not found"));
            }

            container.Status = ContainerStatus.Suspended;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Container {ContainerId} Suspended successfully.", containerId);
            return Result.Ok();
        }, "An error occurred while Suspending the container.");
    }

    public async Task<Result<List<ContainerSummaryDto>>> SearchContainersByOwnerNameAsync(int roomCode, string ownerName, CancellationToken cancellationToken = default)
    {
        return await _logger.ExecuteAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(ownerName))
            {
                return await GetAllContainersAsync(roomCode, cancellationToken);
            }

            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomCode == roomCode, cancellationToken);
            if (room == null)
            {
                return Result.Fail(new NotFoundError("Room not found"));
            }

            var containers = await _context.Containers
                .AsNoTracking()
                .Where(c => c.RoomId == room.RoomId && (c.Buyer.FirstName + " " + c.Buyer.LastName).Contains(ownerName))
                .OrderBy(c => c.PurchaseDateTime)
                .Select(container => new ContainerSummaryDto
                {
                    ContainerId = container.ContainerId,
                    ContainerName = container.ContainerName,
                    PurchaseDateTime = container.PurchaseDateTime,
                    BuyerId = container.BuyerId,
                    BuyerName = container.Buyer.Name,
                    TotalQuantity = container.TotalQuantity,
                    RemainingQuantity = container.RemainingQuantity,
                    Amount = container.Amount,
                    RoomName = container.Room.RoomName,
                    Status = container.Status,
                    Price = container.Price,
                    DeletedAt = container.DeletedAt,
                    DeletionReason = container.DeletionReason
                })
                .ToListAsync(cancellationToken);

            return Result.Ok(containers);
        }, "An error occurred while searching containers.");
    }

    public async Task<Result<List<ContainerSummaryDto>>> GetMyContainers(Guid userId, int roomCode, CancellationToken cancellationToken = default)
    {
        return await _logger.ExecuteAsync(async () =>
        {
            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomCode == roomCode, cancellationToken);
            if (room == null)
            {
                return Result.Fail(new NotFoundError("Room not found"));
            }

            var containers = await _context.Containers
                .AsNoTracking()
                .Where(c => c.RoomId == room.RoomId && c.BuyerId == userId)
                .OrderBy(c => c.PurchaseDateTime)
                .Select(container => new ContainerSummaryDto
                {
                    ContainerId = container.ContainerId,
                    ContainerName = container.ContainerName,
                    PurchaseDateTime = container.PurchaseDateTime,
                    BuyerId = container.BuyerId,
                    BuyerName = container.Buyer.Name,
                    TotalQuantity = container.TotalQuantity,
                    RemainingQuantity = container.RemainingQuantity,
                    Amount = container.Amount,
                    RoomName = container.Room.RoomName,
                    Status = container.Status,
                    Price = container.Price,
                    DeletedAt = container.DeletedAt,
                    DeletionReason = container.DeletionReason
                })
                .ToListAsync(cancellationToken);

            return Result.Ok(containers);
        }, "An error occurred while getting containers.");
    }

    public async Task<Result<List<ContainerSummaryDto>>> GetPagedContainersAsync(int roomCode, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _logger.ExecuteAsync(async () =>
        {
            // Validate pagination parameters
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100); // Limit page size to 100

            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomCode == roomCode, cancellationToken);
            if (room == null)
            {
                return Result.Fail(new NotFoundError("Room not found"));
            }

            var containers = await _context.Containers
                .AsNoTracking()
                .Where(c => c.RoomId == room.RoomId)
                .OrderBy(c => c.PurchaseDateTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(container => new ContainerSummaryDto
                {
                    ContainerId = container.ContainerId,
                    ContainerName = container.ContainerName,
                    PurchaseDateTime = container.PurchaseDateTime,
                    BuyerId = container.BuyerId,
                    BuyerName = container.Buyer.Name,
                    TotalQuantity = container.TotalQuantity,
                    RemainingQuantity = container.RemainingQuantity,
                    Amount = container.Amount,
                    RoomName = container.Room.RoomName,
                    Status = container.Status,
                    Price = container.Price,
                    DeletedAt = container.DeletedAt,
                    DeletionReason = container.DeletionReason
                })
                .ToListAsync(cancellationToken);

            return Result.Ok(containers);
        }, "An error occurred while retrieving paged containers.");
    }

    public async Task<Result<ContainerSummaryDto>> CreateContainerAsync(int roomCode, ContainerCreateDto dto, CancellationToken cancellationToken = default)
    {
        return await _logger.ExecuteAsync(async () =>
        {
            // Find room by room code
            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomCode == roomCode, cancellationToken);
            if (room == null)
            {
                return Result.Fail(new NotFoundError("Room not found"));
            }

            // Validate buyer exists
            var buyer = await _context.Users.FindAsync([dto.BuyerId], cancellationToken);
            if (buyer == null)
            {
                return Result.Fail(new NotFoundError("Buyer not found"));
            }

            var container = new Container
            {
                ContainerId = Guid.NewGuid(),
                ContainerName = dto.ContainerName,
                TotalQuantity = dto.TotalQuantity,
                RemainingQuantity = dto.TotalQuantity,
                Amount = dto.Amount,
                BuyerId = dto.BuyerId,
                RoomId = room.RoomId,
                ResourceTypeId = ResourceType.EggsId,
                PurchaseDateTime = DateTime.UtcNow,
                Status = ContainerStatus.Available,
            };

            _context.Containers.Add(container);
            await _context.SaveChangesAsync(cancellationToken);

            var result = new ContainerSummaryDto
            {
                ContainerId = container.ContainerId,
                ContainerName = container.ContainerName,
                PurchaseDateTime = container.PurchaseDateTime,
                BuyerId = container.BuyerId,
                BuyerName = buyer.Name,
                TotalQuantity = container.TotalQuantity,
                RemainingQuantity = container.RemainingQuantity,
                Amount = container.Amount,
                Status = container.Status,
                Price = container.Price,
                RoomName = room.RoomName,
                DeletedAt = container.DeletedAt,
                DeletionReason = container.DeletionReason
            };

            _logger.LogInformation("Created container {ContainerName} with ID {ContainerId} in room {RoomName}", container.ContainerName, container.ContainerId, room.RoomName);
            return Result.Ok(result);
        }, "Failed to create container");
    }
}
