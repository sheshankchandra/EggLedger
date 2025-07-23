using EggLedger.Data;
using EggLedger.DTO.Room;
using EggLedger.DTO.User;
using EggLedger.Models.Models;
using EggLedger.Models.Enums;
using EggLedger.Services.Interfaces;
using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EggLedger.Services.Services
{
    public class RoomService : IRoomService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RoomService> _logger;
        private readonly IHelperService _helperService;

        public RoomService(ApplicationDbContext context, ILogger<RoomService> logger, IHelperService helperService)
        {
            _context = context;
            _logger = logger;
            _helperService = helperService;
        }

        public async Task<Result<int>> CreateRoomAsync(Guid userId, CreateRoomDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var room = new Room()
                {
                    Id = Guid.NewGuid(),
                    Name = dto.RoomName,
                    Code = _helperService.GenerateNewRoomCode(),
                    IsPublic = dto.IsOpen,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId,
                    Status = RoomStatus.Active
                };

                var userRoom = new UserRoom
                {
                    Id = Guid.NewGuid(),
                    RoomId = room.Id,
                    UserId = userId,
                    IsAdmin = true,
                    JoinedAt = DateTime.UtcNow
                };

                _context.Rooms.Add(room);
                _context.UserRooms.Add(userRoom);
                await _context.SaveChangesAsync();

                _logger.LogInformation("New Room {Name} Created: {Id}", room.Name, room.Id);

                return Result.Ok(room.Code);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "CreateRoomAsync was canceled for userId {Id}", userId);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in CreateRoomAsync for userId {Id}", userId);
                return Result.Fail("An error occurred while creating the room.");
            }
        }

        public async Task<Result<int>> JoinRoomAsync(Guid userId, int roomCode, CancellationToken cancellationToken = default)
        {
            try
            {
                var room = await _context.Rooms
                    .Include(room => room.UserRooms)
                    .Where(r => r.Code == roomCode && r.Status == RoomStatus.Active)
                    .FirstOrDefaultAsync(cancellationToken);

                if (room == null)
                {
                    _logger.LogWarning("Active room not found, code '{Code}'", roomCode);
                    return Result.Fail("Room not found");
                }

                if (room.UserRooms.Any(ur => ur.UserId == userId))
                {
                    _logger.LogError("User : {Id} already in room : {Name}", userId, room.Name);
                    return Result.Fail("User already in room");
                }

                if (!room.IsPublic)
                {
                    _logger.LogWarning("Room is not Public, code '{Code}'", roomCode);
                    return Result.Fail("Room is not Public");
                }

                var userRoom = new UserRoom
                {
                    Id = Guid.NewGuid(),
                    RoomId = room.Id,
                    UserId = userId,
                    IsAdmin = false,
                    JoinedAt = DateTime.UtcNow
                };

                _context.UserRooms.Add(userRoom);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {Id} successfully joined Room {Name}", userId, room.Name);

                return Result.Ok(roomCode);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "JoinRoomAsync was canceled for userId {Id}, roomCode {Code}", userId, roomCode);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in JoinRoomAsync for userId {Id}, roomCode {Code}", userId, roomCode);
                return Result.Fail("An error occurred while joining the room.");
            }
        }

        public async Task<Result<List<UserSummaryDto>>> GetAllRoomUsersAsync(int roomCode, CancellationToken cancellationToken = default)
        {
            try
            {
                var room = await _context.Rooms
                    .Where(r => r.Code == roomCode && r.Status == RoomStatus.Active)
                    .FirstOrDefaultAsync(cancellationToken);

                if (room == null)
                {
                    return Result.Fail("Room not found");
                }

                var users = await _context.Users.AsNoTracking()
                    .Include(u => u.UserRooms)
                    .Where(u => u.UserRooms.Any(ur => ur.RoomId == room.Id))
                    .Select(u => new UserSummaryDto
                    {
                        UserId = u.Id,
                        Name = u.Name,
                        Email = u.Email,
                        Role = u.Role
                    })
                    .ToListAsync(cancellationToken);

                return Result.Ok(users);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "GetAllRoomUsersAsync was canceled for roomCode {Code}", roomCode);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetAllRoomUsersAsync for roomCode {Code}", roomCode);
                return Result.Fail("An error occurred while retrieving room users.");
            }
        }

        public async Task<Result<string>> UpdateRoomPublicStatusAsync(UpdateRoomPublicStatusDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                UserRoom? userRoom = await _context.UserRooms
                    .Include(ur => ur.Room)
                    .Where(ur => ur.RoomId == dto.RoomId && ur.UserId == dto.UserId && ur.Room.Status == RoomStatus.Active)
                    .FirstOrDefaultAsync(cancellationToken);

                if (userRoom == null)
                {
                    _logger.LogError("Active room '{Id}' not found or user '{Id}' is not in that room", dto.RoomId, dto.UserId);
                    return Result.Fail("Room not found or user is not in that room");
                }

                if (!userRoom.IsAdmin)
                {
                    _logger.LogWarning("User '{Id}' is not admin of room '{Id}'", dto.UserId, dto.RoomId);
                    return Result.Fail("Only room admin can update visibility");
                }

                Room room = userRoom.Room;

                if (room.IsPublic == dto.IsOpen)
                {
                    _logger.LogInformation("Room '{Name}' is already {Status}", room.Name, dto.IsOpen ? "public" : "private");
                    return Result.Ok($"Room is already {(dto.IsOpen ? "public" : "private")}");
                }

                room.IsPublic = dto.IsOpen;
                room.ModifiedAt = DateTime.UtcNow;
                room.ModifiedBy = dto.UserId;

                _logger.LogInformation("Updated room '{Name}' visibility to {IsPublic}", room.Name, room.IsPublic);

                await _context.SaveChangesAsync();
                return Result.Ok("Room visibility updated successfully");
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "UpdateRoomPublicStatusAsync was canceled for roomId {Id}", dto.RoomId);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating room visibility for Id {Id}", dto.RoomId);
                return Result.Fail("Unexpected error occurred while updating the room's public status");
            }
        }

        public async Task<Result<List<RoomDto>>> GetAllUserRoomsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var userRooms = await _context.UserRooms
                    .AsNoTracking()
                    .Include(ur => ur.Room)
                    .Where(ur => ur.UserId == userId && ur.Room.Status == RoomStatus.Active)
                    .Select(ur => new RoomDto
                    {
                        RoomId = ur.Room.Id,
                        RoomName = ur.Room.Name,
                        RoomCode = ur.Room.Code,
                        IsOpen = ur.Room.IsPublic,
                        AdminUserId = ur.IsAdmin ? userId : null,
                        CreateAt = ur.Room.CreatedAt,
                        ContainerCount = _context.Containers.Count(c => c.RoomId == ur.Room.Id && c.Status != ContainerStatus.Archived),
                        TotalEggs = _context.Containers
                            .Where(c => c.RoomId == ur.Room.Id && c.Status != ContainerStatus.Archived)
                            .Sum(c => c.RemainingQuantity),
                        MemberCount = _context.UserRooms.Count(ur2 => ur2.RoomId == ur.Room.Id)
                    })
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Retrieved {Count} active rooms for user {Id}", userRooms.Count, userId);
                return Result.Ok(userRooms);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "GetAllUserRoomsAsync was canceled for userId {Id}", userId);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting rooms for user {Id}", userId);
                return Result.Fail("Failed to retrieve user rooms");
            }
        }

        public async Task<Result<RoomDto>> GetRoomByCodeAsync(int roomCode, CancellationToken cancellationToken = default)
        {
            try
            {
                var room = await _context.Rooms
                    .AsNoTracking()
                    .Where(r => r.Code == roomCode && r.Status == RoomStatus.Active)
                    .Include(r => r.UserRooms)
                    .Select(r => new RoomDto
                    {
                        RoomId = r.Id,
                        RoomName = r.Name,
                        RoomCode = r.Code,
                        IsOpen = r.IsPublic,
                        AdminUserId = r.UserRooms.Where(ur => ur.IsAdmin).Select(ur => ur.UserId).FirstOrDefault(),
                        CreateAt = r.CreatedAt,
                        ContainerCount = _context.Containers.Count(c => c.RoomId == r.Id && c.Status != ContainerStatus.Archived),
                        TotalEggs = _context.Containers
                            .Where(c => c.RoomId == r.Id && c.Status != ContainerStatus.Archived)
                            .Sum(c => c.RemainingQuantity),
                        MemberCount = _context.UserRooms.Count(ur2 => ur2.RoomId == r.Id)
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (room == null)
                {
                    _logger.LogWarning("Active room with code {Code} not found", roomCode);
                    return Result.Fail<RoomDto>("Room not found");
                }

                _logger.LogInformation("Retrieved room {Code}", roomCode);
                return Result.Ok(room);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "GetRoomByCodeAsync was canceled for roomCode {Code}", roomCode);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving room {Code}", roomCode);
                return Result.Fail("Failed to retrieve room");
            }
        }

        public async Task<Result<int>> DeleteRoomAsync(int roomCode, Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Single query to get room with user validation
                var roomWithUserValidation = await _context.Rooms
                    .Where(r => r.Code == roomCode && r.Status == RoomStatus.Active)
                    .Select(r => new
                    {
                        Room = r,
                        UserRoom = r.UserRooms.FirstOrDefault(ur => ur.UserId == userId),
                        ContainerCount = r.Containers.Count(c => c.Status != ContainerStatus.Archived),
                        ActiveOrderDetailsCount = _context.OrderDetails
                            .Count(od => od.Container.RoomId == r.Id && 
                                        od.Container.Status != ContainerStatus.Archived &&
                                        od.Status != OrderDetailStatus.Completed)
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (roomWithUserValidation?.Room == null)
                {
                    _logger.LogError("Unable to find active room with code: {Code}", roomCode);
                    return Result.Fail("Unable to find the Room");
                }

                if (roomWithUserValidation.UserRoom == null)
                {
                    _logger.LogError("User : {Id} not found in the Room : {Code}", userId, roomCode);
                    return Result.Fail("User not found in the Room");
                }

                if (!roomWithUserValidation.UserRoom.IsAdmin)
                {
                    _logger.LogError("User : {Id} is not Admin for the Room : {Code}", userId, roomCode);
                    return Result.Fail("User is not Admin for the Room");
                }

                // Check for active orders that would be affected
                if (roomWithUserValidation.ActiveOrderDetailsCount > 0)
                {
                    _logger.LogWarning("Cannot archive room {Code} - has {Count} active order details", 
                        roomCode, roomWithUserValidation.ActiveOrderDetailsCount);
                    return Result.Fail("Cannot archive room with active orders. Please complete or cancel all orders first.");
                }

                var room = roomWithUserValidation.Room;
                
                _logger.LogInformation("Archiving room {Name} (Code: {Code}) and associated containers",
                    room.Name, roomCode);

                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                
                try
                {
                    var archiveTime = DateTime.UtcNow;
                    int totalAffectedRows = 0;

                    // Archive containers (preserves order history)
                    var containersToArchive = await _context.Containers
                        .Where(c => c.RoomId == room.Id && c.Status != ContainerStatus.Archived)
                        .ToListAsync(cancellationToken);

                    foreach (var container in containersToArchive)
                    {
                        container.Status = ContainerStatus.Archived;
                        container.DeletedAt = archiveTime;
                        container.DeletedBy = userId;
                        container.DeletionReason = "Room archived";
                        container.ModifiedAt = archiveTime;
                        container.ModifiedBy = userId;
                    }
                    totalAffectedRows += containersToArchive.Count;

                    // Remove user memberships (this is safe to hard delete)
                    var userRoomsDeleted = await _context.UserRooms
                        .Where(ur => ur.RoomId == room.Id)
                        .ExecuteDeleteAsync(cancellationToken);
                    totalAffectedRows += userRoomsDeleted;

                    // Archive the room
                    room.Status = RoomStatus.Archived;
                    room.DeletedAt = archiveTime;
                    room.DeletedBy = userId;
                    room.DeletionReason = "Room archived by admin";
                    room.ModifiedAt = archiveTime;
                    room.ModifiedBy = userId;
                    totalAffectedRows += 1;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync(cancellationToken);

                    _logger.LogInformation("Successfully archived room {Code}. " +
                        "Containers archived: {ContainersArchived}, UserRooms removed: {UserRoomsDeleted}", 
                        roomCode, containersToArchive.Count, userRoomsDeleted);

                    return Result.Ok(totalAffectedRows);
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("DeleteRoomAsync was canceled for room code {Code}", roomCode);
                return Result.Fail<int>("Operation was canceled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while archiving room with code {Code}", roomCode);
                return Result.Fail<int>("An error occurred while archiving the room");
            }
        }

    }
}
