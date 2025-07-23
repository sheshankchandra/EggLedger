using EggLedger.Data;
using EggLedger.DTO.Container;
using EggLedger.Models.Enums;
using EggLedger.Models.Models;
using EggLedger.Services.Interfaces;
using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EggLedger.Services.Services
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
        /// Retrieves all active containers with summary information for a specific room.
        /// </summary>
        public async Task<Result<List<ContainerSummaryDto>>> GetAllContainersAsync(int roomCode, CancellationToken cancellationToken = default)
        {
            try
            {
                var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Code == roomCode, cancellationToken);
                if (room == null)
                    return Result.Fail("Room not found");

                var containersList = await _context.Containers
                    .AsNoTracking()
                    .Where(c => c.RoomId == room.Id && c.Status == ContainerStatus.Available && c.RemainingQuantity > 0)
                    .OrderBy(c => c.PurchaseDateTime)
                    .Select(c => new ContainerSummaryDto
                    {
                        ContainerId = c.Id,
                        ContainerName = c.Name,
                        PurchaseDateTime = c.PurchaseDateTime,
                        BuyerName = c.Buyer.Name,
                        TotalQuantity = c.TotalQuantity,
                        RemainingQuantity = c.RemainingQuantity,
                        Amount = c.Amount,  
                        RoomName = c.Room.Name,
                        Status = c.Status,
                        Price = c.Price,
                        CompletedDateTime = c.CompletedDateTime
                    })
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Found {Count} containers in room {Name}.", containersList.Count, room.Name);
                return Result.Ok(containersList);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "GetAllContainersAsync was canceled for roomCode {Code}", roomCode);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetAllContainersAsync for roomCode {Code}", roomCode);
                return Result.Fail("An error occurred while retrieving containers.");
            }
        }

        /// <summary>
        /// Retrieves a single container by its ID with summary information.
        /// </summary>
        public async Task<Result<ContainerSummaryDto>> GetContainerAsync(Guid containerId, CancellationToken cancellationToken = default)
        {
            try
            {
                var container = await _context.Containers
                    .Include(container => container.Buyer)
                    .Include(container => container.Room)
                    .FirstOrDefaultAsync(c => c.Id == containerId, cancellationToken);

                if (container == null)
                {
                    _logger.LogWarning("Container with ID {Id} not found.", containerId);
                    return Result.Fail("Container not found");
                }

                var summaryDto = new ContainerSummaryDto
                {
                    ContainerId = container.Id,
                    ContainerName = container.Name,
                    PurchaseDateTime = container.PurchaseDateTime,
                    BuyerName = container.Buyer.Name,
                    TotalQuantity = container.TotalQuantity,
                    RemainingQuantity = container.RemainingQuantity,
                    Amount = container.Amount,
                    RoomName = container.Room.Name,
                    Status = container.Status,
                    Price = container.Price,
                    CompletedDateTime = container.CompletedDateTime
                };

                _logger.LogInformation("Container {Name} retrieved successfully.", summaryDto.ContainerName);
                return Result.Ok(summaryDto);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "GetContainerAsync was canceled for containerId {Id}", containerId);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetContainerAsync for containerId {Id}", containerId);
                return Result.Fail("An error occurred while retrieving the container.");
            }
        }

        public async Task<Result<ContainerSummaryDto>> UpdateContainerAsync(Guid containerId, ContainerUpdateDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var container = await _context.Containers
                    .Include(container => container.Room)
                    .Include(container => container.Buyer)
                    .FirstOrDefaultAsync(c => c.Id == containerId, cancellationToken);

                if (container == null)
                {
                    _logger.LogWarning("Container with ID {Id} not found.", containerId);
                    return Result.Fail("Container not found");
                }

                // Update only provided properties
                container.Name = dto.ContainerName ?? container.Name;
                container.PurchaseDateTime = dto.PurchaseDateTime ?? container.PurchaseDateTime;
                container.TotalQuantity = dto.TotalQuantity ?? container.TotalQuantity;
                container.RemainingQuantity = dto.RemainingQuantity ?? container.RemainingQuantity;
                container.Amount = dto.Amount ?? container.Amount;

                await _context.SaveChangesAsync();

                var containerDto = new ContainerSummaryDto
                {
                    ContainerId = container.Id,
                    ContainerName = container.Name,
                    PurchaseDateTime = container.PurchaseDateTime,
                    BuyerName = container.Buyer.Name,
                    TotalQuantity = container.TotalQuantity,
                    RemainingQuantity = container.RemainingQuantity,
                    Amount = container.Amount,
                    RoomName = container.Room.Name,
                    Status = container.Status,
                    Price = container.Price,
                    CompletedDateTime = container.CompletedDateTime
                };

                _logger.LogInformation("Container {Id} updated successfully.", container.Id);
                return Result.Ok(containerDto);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "UpdateContainerAsync was canceled for containerId {Id}", containerId);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in UpdateContainerAsync for containerId {Id}", containerId);
                return Result.Fail("An error occurred while updating the container.");
            }
        }

        public async Task<Result> ArchiveContainerAsync(Guid containerId, CancellationToken cancellationToken = default)
        {
            try
            {
                var container = await _context.Containers
                    .FirstOrDefaultAsync(c => c.Id == containerId, cancellationToken);

                if (container == null)
                {
                    _logger.LogWarning("Container with ID {Id} not found.", containerId);
                    return Result.Fail("Container not found");
                }

                container.Status = ContainerStatus.Archived;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Container {Id} Archived successfully.", containerId);
                return Result.Ok();
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "ArchiveContainerAsync was canceled for containerId {Id}", containerId);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in ArchiveContainerAsync for containerId {Id}", containerId);
                return Result.Fail("An error occurred while Archiving the container.");
            }
        }

        public async Task<Result> SuspendContainerAsync(Guid containerId, CancellationToken cancellationToken = default)
        {
            try
            {
                var container = await _context.Containers
                    .FirstOrDefaultAsync(c => c.Id == containerId, cancellationToken);

                if (container == null)
                {
                    _logger.LogWarning("Container with ID {Id} not found.", containerId);
                    return Result.Fail("Container not found");
                }

                container.Status = ContainerStatus.Suspended;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Container {Id} Suspended successfully.", containerId);
                return Result.Ok();
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "SuspendContainerAsync was canceled for containerId {Id}", containerId);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in SuspendContainerAsync for containerId {Id}", containerId);
                return Result.Fail("An error occurred while Suspending the container.");
            }
        }

        public async Task<Result<List<ContainerSummaryDto>>> SearchContainersByOwnerNameAsync(int roomCode, string ownerName, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ownerName))
                {
                    return await GetAllContainersAsync(roomCode, cancellationToken);
                }

                var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Code == roomCode, cancellationToken);
                if (room == null)
                {
                    return Result.Fail("Room not found");
                }

                var containers = await _context.Containers
                    .AsNoTracking()
                    .Where(c => c.RoomId == room.Id && (c.Buyer.FirstName + " " + c.Buyer.LastName).Contains(ownerName))
                    .OrderBy(c => c.PurchaseDateTime)
                    .Select(container => new ContainerSummaryDto
                    {
                        ContainerId = container.Id,
                        ContainerName = container.Name,
                        PurchaseDateTime = container.PurchaseDateTime,
                        BuyerName = container.Buyer.Name,
                        TotalQuantity = container.TotalQuantity,
                        RemainingQuantity = container.RemainingQuantity,
                        Amount = container.Amount,
                        RoomName = container.Room.Name,
                        Status = container.Status,
                        Price = container.Price,
                        CompletedDateTime = container.CompletedDateTime
                    })
                    .ToListAsync(cancellationToken);

                return Result.Ok(containers);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "SearchContainersByOwnerNameAsync was canceled for roomCode {Code}, ownerName {OwnerName}", roomCode, ownerName);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in SearchContainersByOwnerNameAsync for roomCode {Code}, ownerName {OwnerName}", roomCode, ownerName);
                return Result.Fail("An error occurred while searching containers.");
            }
        }

        public async Task<Result<List<ContainerSummaryDto>>> GetMyContainers(Guid userId, int roomCode, CancellationToken cancellationToken = default)
        {
            try
            {
                var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Code == roomCode, cancellationToken);
                if (room == null)
                {
                    return Result.Fail("Room not found");
                }

                var containers = await _context.Containers
                    .AsNoTracking()
                    .Where(c => c.RoomId == room.Id && c.BuyerId == userId)
                    .OrderBy(c => c.PurchaseDateTime)
                    .Select(container => new ContainerSummaryDto
                    {
                        ContainerId = container.Id,
                        ContainerName = container.Name,
                        PurchaseDateTime = container.PurchaseDateTime,
                        BuyerName = container.Buyer.Name,
                        TotalQuantity = container.TotalQuantity,
                        RemainingQuantity = container.RemainingQuantity,
                        Amount = container.Amount,
                        RoomName = container.Room.Name,
                        Status = container.Status,
                        Price = container.Price,
                        CompletedDateTime = container.CompletedDateTime
                    })
                    .ToListAsync(cancellationToken);

                return Result.Ok(containers);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "GetMyContainers was canceled for roomCode {Code}, BuyerId {BuyerId}", roomCode, userId);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetMyContainers for roomCode {Code}, BuyerId {BuyerId}", roomCode, userId);
                return Result.Fail("An error occurred while getting containers.");
            }
        }

        public async Task<Result<List<ContainerSummaryDto>>> GetPagedContainersAsync(int roomCode, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate pagination parameters
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 100); // Limit page size to 100

                var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Code == roomCode, cancellationToken);
                if (room == null)
                {
                    return Result.Fail("Room not found");
                }

                var containers = await _context.Containers
                    .AsNoTracking()
                    .Where(c => c.RoomId == room.Id)
                    .OrderBy(c => c.PurchaseDateTime)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(container => new ContainerSummaryDto
                    {
                        ContainerId = container.Id,
                        ContainerName = container.Name,
                        PurchaseDateTime = container.PurchaseDateTime,
                        BuyerName = container.Buyer.Name,
                        TotalQuantity = container.TotalQuantity,
                        RemainingQuantity = container.RemainingQuantity,
                        Amount = container.Amount,
                        RoomName = container.Room.Name,
                        Status = container.Status,
                        Price = container.Price,
                        CompletedDateTime = container.CompletedDateTime
                    })
                    .ToListAsync(cancellationToken);

                return Result.Ok(containers);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "GetPagedContainersAsync was canceled for roomCode {Code}, page {Page}, pageSize {PageSize}", roomCode, page, pageSize);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetPagedContainersAsync for roomCode {Code}, page {Page}, pageSize {PageSize}", roomCode, page, pageSize);
                return Result.Fail("An error occurred while retrieving paged containers.");
            }
        }

        public async Task<Result<ContainerSummaryDto>> CreateContainerAsync(int roomCode, ContainerCreateDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                // Find room by room code
                var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Code == roomCode, cancellationToken);
                if (room == null)
                {
                    return Result.Fail("Room not found");
                }

                // Validate buyer exists
                var buyer = await _context.Users.FindAsync([dto.BuyerId], cancellationToken);
                if (buyer == null)
                {
                    return Result.Fail("Buyer not found");
                }

                var container = new Container
                {
                    Id = Guid.NewGuid(),
                    Name = dto.ContainerName,
                    TotalQuantity = dto.TotalQuantity,
                    RemainingQuantity = dto.TotalQuantity,
                    Amount = dto.Amount,
                    BuyerId = dto.BuyerId,
                    RoomId = room.Id,
                    PurchaseDateTime = DateTime.UtcNow,
                    Status = ContainerStatus.Available,
                };

                _context.Containers.Add(container);
                await _context.SaveChangesAsync();

                var result = new ContainerSummaryDto
                {
                    ContainerId = container.Id,
                    ContainerName = container.Name,
                    PurchaseDateTime = container.PurchaseDateTime,
                    BuyerName = buyer.Name,
                    TotalQuantity = container.TotalQuantity,
                    RemainingQuantity = container.RemainingQuantity,
                    Amount = container.Amount,
                    Status = container.Status,
                    Price = container.Price,
                    RoomName = room.Name,
                    CompletedDateTime = container.CompletedDateTime
                };

                _logger.LogInformation("Created container {Name} with ID {Id} in room {Name}", container.Name, container.Id, room.Name);
                return Result.Ok(result);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "CreateContainerAsync was canceled for roomCode {Code}", roomCode);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating container for roomCode {Code}", roomCode);
                return Result.Fail("Failed to create container");
            }
        }
    }
}