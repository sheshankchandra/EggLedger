using EggLedger.Data;
using EggLedger.DTO.Container;
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
        /// Retrieves all containers with summary information for a specific room.
        /// </summary>
        public async Task<Result<List<ContainerSummaryDto>>> GetAllContainersAsync(int roomCode, CancellationToken cancellationToken = default)
        {
            try
            {
                var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomCode == roomCode, cancellationToken);
                if (room == null)
                    return Result.Fail("Room not found");

                var containersList = await _context.Containers
                    .AsNoTracking()
                    .Where(c => c.RoomId == room.RoomId)
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
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Found {Count} containers in room {RoomName}.", containersList.Count, room.RoomName);
                return Result.Ok(containersList);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "GetAllContainersAsync was canceled for roomCode {RoomCode}", roomCode);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetAllContainersAsync for roomCode {RoomCode}", roomCode);
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
                    .FirstOrDefaultAsync(c => c.ContainerId == containerId, cancellationToken);

                if (container == null)
                {
                    _logger.LogWarning("Container with ID {ContainerId} not found.", containerId);
                    return Result.Fail("Container not found");
                }

                var summaryDto = new ContainerSummaryDto
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
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "GetContainerAsync was canceled for containerId {ContainerId}", containerId);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetContainerAsync for containerId {ContainerId}", containerId);
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
                    .FirstOrDefaultAsync(c => c.ContainerId == containerId, cancellationToken);

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

                await _context.SaveChangesAsync(cancellationToken);

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
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "UpdateContainerAsync was canceled for containerId {ContainerId}", containerId);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in UpdateContainerAsync for containerId {ContainerId}", containerId);
                return Result.Fail("An error occurred while updating the container.");
            }
        }

        public async Task<Result> DeleteContainerAsync(Guid containerId, CancellationToken cancellationToken = default)
        {
            try
            {
                var container = await _context.Containers
                    .FirstOrDefaultAsync(c => c.ContainerId == containerId, cancellationToken);

                if (container == null)
                {
                    _logger.LogWarning("Container with ID {ContainerId} not found.", containerId);
                    return Result.Fail("Container not found");
                }

                _context.Containers.Remove(container);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Container {ContainerId} deleted successfully.", containerId);
                return Result.Ok();
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "DeleteContainerAsync was canceled for containerId {ContainerId}", containerId);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in DeleteContainerAsync for containerId {ContainerId}", containerId);
                return Result.Fail("An error occurred while deleting the container.");
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

                var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomCode == roomCode, cancellationToken);
                if (room == null)
                {
                    return Result.Fail("Room not found");
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
                        BuyerName = container.Buyer.Name,
                        TotalQuantity = container.TotalQuantity,
                        RemainingQuantity = container.RemainingQuantity,
                        Amount = container.Amount,
                        RoomName = container.Room.RoomName,
                        Price = container.Price,
                        CompletedDateTime = container.CompletedDateTime
                    })
                    .ToListAsync(cancellationToken);

                return Result.Ok(containers);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "SearchContainersByOwnerNameAsync was canceled for roomCode {RoomCode}, ownerName {OwnerName}", roomCode, ownerName);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in SearchContainersByOwnerNameAsync for roomCode {RoomCode}, ownerName {OwnerName}", roomCode, ownerName);
                return Result.Fail("An error occurred while searching containers.");
            }
        }

        public async Task<Result<List<ContainerSummaryDto>>> GetPagedContainersAsync(int roomCode, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate pagination parameters
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 100); // Limit page size to 100

                var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomCode == roomCode, cancellationToken);
                if (room == null)
                {
                    return Result.Fail("Room not found");
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
                        BuyerName = container.Buyer.Name,
                        TotalQuantity = container.TotalQuantity,
                        RemainingQuantity = container.RemainingQuantity,
                        Amount = container.Amount,
                        RoomName = container.Room.RoomName,
                        Price = container.Price,
                        CompletedDateTime = container.CompletedDateTime
                    })
                    .ToListAsync(cancellationToken);

                return Result.Ok(containers);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "GetPagedContainersAsync was canceled for roomCode {RoomCode}, page {Page}, pageSize {PageSize}", roomCode, page, pageSize);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetPagedContainersAsync for roomCode {RoomCode}, page {Page}, pageSize {PageSize}", roomCode, page, pageSize);
                return Result.Fail("An error occurred while retrieving paged containers.");
            }
        }

        public async Task<Result<ContainerSummaryDto>> CreateContainerAsync(int roomCode, ContainerCreateDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                // Find room by room code
                var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomCode == roomCode, cancellationToken);
                if (room == null)
                {
                    return Result.Fail("Room not found");
                }

                // Validate buyer exists
                var buyer = await _context.Users.FindAsync(new object[] { dto.BuyerId }, cancellationToken);
                if (buyer == null)
                {
                    return Result.Fail("Buyer not found");
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
                    PurchaseDateTime = DateTime.UtcNow,
                };

                _context.Containers.Add(container);
                await _context.SaveChangesAsync(cancellationToken);

                var result = new ContainerSummaryDto
                {
                    ContainerId = container.ContainerId,
                    ContainerName = container.ContainerName,
                    PurchaseDateTime = container.PurchaseDateTime,
                    BuyerName = buyer.Name,
                    TotalQuantity = container.TotalQuantity,
                    RemainingQuantity = container.RemainingQuantity,
                    Amount = container.Amount,
                    Price = container.Price,
                    RoomName = room.RoomName,
                    CompletedDateTime = container.CompletedDateTime
                };

                _logger.LogInformation("Created container {ContainerName} with ID {ContainerId} in room {RoomName}", container.ContainerName, container.ContainerId, room.RoomName);
                return Result.Ok(result);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "CreateContainerAsync was canceled for roomCode {RoomCode}", roomCode);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating container for roomCode {RoomCode}", roomCode);
                return Result.Fail("Failed to create container");
            }
        }
    }
}