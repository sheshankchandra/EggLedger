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
                    RoomId = Guid.NewGuid(),
                    RoomName = dto.RoomName,
                    RoomCode = _helperService.GenerateNewRoomCode(),
                    IsPublic = dto.IsOpen,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId,
                    Status = RoomStatus.Active
                };

                var userRoom = new UserRoom
                {
                    Id = Guid.NewGuid(),
                    RoomId = room.RoomId,
                    UserId = userId,
                    IsAdmin = true,
                    JoinedAt = DateTime.UtcNow
                };

                _context.Rooms.Add(room);
                _context.UserRooms.Add(userRoom);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("New Room {RoomName} Created: {RoomId}", room.RoomName, room.RoomId);

                return Result.Ok(room.RoomCode);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "CreateRoomAsync was canceled for userId {UserId}", userId);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in CreateRoomAsync for userId {UserId}", userId);
                return Result.Fail("An error occurred while creating the room.");
            }
        }

        public async Task<Result<int>> JoinRoomAsync(Guid userId, int roomCode, CancellationToken cancellationToken = default)
        {
            try
            {
                var room = await _context.Rooms
                    .Include(room => room.UserRooms)
                    .Where(r => r.RoomCode == roomCode && r.Status == RoomStatus.Active)
                    .FirstOrDefaultAsync(cancellationToken);

                if (room == null)
                {
                    _logger.LogWarning("Active room not found, code '{RoomCode}'", roomCode);
                    return Result.Fail("Room not found");
                }

                if (room.UserRooms.Any(ur => ur.UserId == userId))
                {
                    _logger.LogError("User : {UserId} already in room : {RoomName}", userId, room.RoomName);
                    return Result.Fail("User already in room");
                }

                if (!room.IsPublic)
                {
                    _logger.LogWarning("Room is not Public, code '{RoomCode}'", roomCode);
                    return Result.Fail("Room is not Public");
                }

                var userRoom = new UserRoom
                {
                    Id = Guid.NewGuid(),
                    RoomId = room.RoomId,
                    UserId = userId,
                    IsAdmin = false,
                    JoinedAt = DateTime.UtcNow
                };

                _context.UserRooms.Add(userRoom);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("User {UserId} successfully joined Room {RoomName}", userId, room.RoomName);

                return Result.Ok(roomCode);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "JoinRoomAsync was canceled for userId {UserId}, roomCode {RoomCode}", userId, roomCode);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in JoinRoomAsync for userId {UserId}, roomCode {RoomCode}", userId, roomCode);
                return Result.Fail("An error occurred while joining the room.");
            }
        }

        public async Task<Result<List<UserSummaryDto>>> GetAllRoomUsersAsync(int roomCode, CancellationToken cancellationToken = default)
        {
            try
            {
                var room = await _context.Rooms
                    .Where(r => r.RoomCode == roomCode && r.Status == RoomStatus.Active)
                    .FirstOrDefaultAsync(cancellationToken);

                if (room == null)
                {
                    return Result.Fail("Room not found");
                }

                var users = await _context.Users.AsNoTracking()
                    .Include(u => u.UserRooms)
                    .Where(u => u.UserRooms.Any(ur => ur.RoomId == room.RoomId))
                    .Select(u => new UserSummaryDto
                    {
                        UserId = u.UserId,
                        Name = u.Name,
                        Email = u.Email,
                        Role = u.Role
                    })
                    .ToListAsync(cancellationToken);

                return Result.Ok(users);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "GetAllRoomUsersAsync was canceled for roomCode {RoomCode}", roomCode);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetAllRoomUsersAsync for roomCode {RoomCode}", roomCode);
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
                    _logger.LogError("Active room '{RoomId}' not found or user '{UserId}' is not in that room", dto.RoomId, dto.UserId);
                    return Result.Fail("Room not found or user is not in that room");
                }

                if (!userRoom.IsAdmin)
                {
                    _logger.LogWarning("User '{UserId}' is not admin of room '{RoomId}'", dto.UserId, dto.RoomId);
                    return Result.Fail("Only room admin can update visibility");
                }

                Room room = userRoom.Room;

                if (room.IsPublic == dto.IsOpen)
                {
                    _logger.LogInformation("Room '{RoomName}' is already {Status}", room.RoomName, dto.IsOpen ? "public" : "private");
                    return Result.Ok($"Room is already {(dto.IsOpen ? "public" : "private")}");
                }

                room.IsPublic = dto.IsOpen;
                room.ModifiedAt = DateTime.UtcNow;
                room.ModifiedBy = dto.UserId;

                _logger.LogInformation("Updated room '{RoomName}' visibility to {IsPublic}", room.RoomName, room.IsPublic);

                await _context.SaveChangesAsync(cancellationToken);
                return Result.Ok("Room visibility updated successfully");
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "UpdateRoomPublicStatusAsync was canceled for roomId {RoomId}", dto.RoomId);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating room visibility for RoomId {RoomId}", dto.RoomId);
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
                        RoomId = ur.Room.RoomId,
                        RoomName = ur.Room.RoomName,
                        RoomCode = ur.Room.RoomCode,
                        IsOpen = ur.Room.IsPublic,
                        AdminUserId = ur.IsAdmin ? userId : null,
                        CreateAt = ur.Room.CreatedAt,
                        ContainerCount = _context.Containers.Count(c => c.RoomId == ur.Room.RoomId && c.Status != ContainerStatus.Archived),
                        TotalEggs = _context.Containers
                            .Where(c => c.RoomId == ur.Room.RoomId && c.Status != ContainerStatus.Archived)
                            .Sum(c => c.RemainingQuantity),
                        MemberCount = _context.UserRooms.Count(ur2 => ur2.RoomId == ur.Room.RoomId)
                    })
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Retrieved {Count} active rooms for user {UserId}", userRooms.Count, userId);
                return Result.Ok(userRooms);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "GetAllUserRoomsAsync was canceled for userId {UserId}", userId);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting rooms for user {UserId}", userId);
                return Result.Fail("Failed to retrieve user rooms");
            }
        }

        public async Task<Result<RoomDto>> GetRoomByCodeAsync(int roomCode, CancellationToken cancellationToken = default)
        {
            try
            {
                var room = await _context.Rooms
                    .AsNoTracking()
                    .Where(r => r.RoomCode == roomCode && r.Status == RoomStatus.Active)
                    .Include(r => r.UserRooms)
                    .Select(r => new RoomDto
                    {
                        RoomId = r.RoomId,
                        RoomName = r.RoomName,
                        RoomCode = r.RoomCode,
                        IsOpen = r.IsPublic,
                        AdminUserId = r.UserRooms.Where(ur => ur.IsAdmin).Select(ur => ur.UserId).FirstOrDefault(),
                        CreateAt = r.CreatedAt,
                        ContainerCount = _context.Containers.Count(c => c.RoomId == r.RoomId && c.Status != ContainerStatus.Archived),
                        TotalEggs = _context.Containers
                            .Where(c => c.RoomId == r.RoomId && c.Status != ContainerStatus.Archived)
                            .Sum(c => c.RemainingQuantity),
                        MemberCount = _context.UserRooms.Count(ur2 => ur2.RoomId == r.RoomId)
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (room == null)
                {
                    _logger.LogWarning("Active room with code {RoomCode} not found", roomCode);
                    return Result.Fail<RoomDto>("Room not found");
                }

                _logger.LogInformation("Retrieved room {RoomCode}", roomCode);
                return Result.Ok(room);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "GetRoomByCodeAsync was canceled for roomCode {RoomCode}", roomCode);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving room {RoomCode}", roomCode);
                return Result.Fail("Failed to retrieve room");
            }
        }

        public async Task<Result<int>> DeleteRoomAsync(int roomCode, Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Single query to get room with user validation
                var roomWithUserValidation = await _context.Rooms
                    .Where(r => r.RoomCode == roomCode && r.Status == RoomStatus.Active)
                    .Select(r => new
                    {
                        Room = r,
                        UserRoom = r.UserRooms.FirstOrDefault(ur => ur.UserId == userId),
                        ContainerCount = r.Containers.Count(c => c.Status != ContainerStatus.Archived),
                        ActiveOrderDetailsCount = _context.OrderDetails
                            .Count(od => od.Container.RoomId == r.RoomId && 
                                        od.Container.Status != ContainerStatus.Archived &&
                                        od.OrderDetailStatus != OrderDetailStatus.Completed)
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (roomWithUserValidation?.Room == null)
                {
                    _logger.LogError("Unable to find active room with code: {RoomCode}", roomCode);
                    return Result.Fail("Unable to find the Room");
                }

                if (roomWithUserValidation.UserRoom == null)
                {
                    _logger.LogError("User : {UserId} not found in the Room : {RoomCode}", userId, roomCode);
                    return Result.Fail("User not found in the Room");
                }

                if (!roomWithUserValidation.UserRoom.IsAdmin)
                {
                    _logger.LogError("User : {UserId} is not Admin for the Room : {RoomCode}", userId, roomCode);
                    return Result.Fail("User is not Admin for the Room");
                }

                // Check for active orders that would be affected
                if (roomWithUserValidation.ActiveOrderDetailsCount > 0)
                {
                    _logger.LogWarning("Cannot archive room {RoomCode} - has {Count} active order details", 
                        roomCode, roomWithUserValidation.ActiveOrderDetailsCount);
                    return Result.Fail("Cannot archive room with active orders. Please complete or cancel all orders first.");
                }

                var room = roomWithUserValidation.Room;
                
                _logger.LogInformation("Archiving room {RoomName} (Code: {RoomCode}) and associated containers",
                    room.RoomName, roomCode);

                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                
                try
                {
                    var archiveTime = DateTime.UtcNow;
                    int totalAffectedRows = 0;

                    // Archive containers (preserves order history)
                    var containersToArchive = await _context.Containers
                        .Where(c => c.RoomId == room.RoomId && c.Status != ContainerStatus.Archived)
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
                        .Where(ur => ur.RoomId == room.RoomId)
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

                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    _logger.LogInformation("Successfully archived room {RoomCode}. " +
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
                _logger.LogInformation("DeleteRoomAsync was canceled for room code {RoomCode}", roomCode);
                return Result.Fail<int>("Operation was canceled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while archiving room with code {RoomCode}", roomCode);
                return Result.Fail<int>("An error occurred while archiving the room");
            }
        }

        public async Task<Result<string>> EditRoomNameAsync(Guid userId, Guid roomId, string newRoomName, CancellationToken cancellationToken = default)
        {
            try
            {
                var userRoom = await _context.UserRooms
                    .Include(ur => ur.Room)
                    .Where(ur => ur.RoomId == roomId && ur.UserId == userId && ur.Room.Status == RoomStatus.Active)
                    .FirstOrDefaultAsync(cancellationToken);

                if (userRoom == null)
                {
                    _logger.LogError("Active room '{RoomId}' not found or user '{UserId}' is not in that room", roomId, userId);
                    return Result.Fail("Room not found or user is not in that room");
                }

                if (!userRoom.IsAdmin)
                {
                    _logger.LogWarning("User '{UserId}' is not admin of room '{RoomId}'", userId, roomId);
                    return Result.Fail("Only room admin can edit the room name");
                }

                var room = userRoom.Room;

                if (string.Equals(room.RoomName, newRoomName, StringComparison.Ordinal))
                {
                    _logger.LogInformation("Room '{RoomId}' already has the name '{RoomName}'", roomId, newRoomName);
                    return Result.Ok("Room name is already set to the specified value");
                }

                room.RoomName = newRoomName;
                room.ModifiedAt = DateTime.UtcNow;
                room.ModifiedBy = userId;

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Room name updated for Room '{RoomId}' to '{RoomName}' by User '{UserId}'", roomId, newRoomName, userId);

                return Result.Ok("Room name updated successfully");
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "EditRoomNameAsync was canceled for roomId {RoomId}", roomId);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while editing room name for RoomId {RoomId}", roomId);
                return Result.Fail("Unexpected error occurred while editing the room name");
            }
        }

        public async Task<Result<string>> RemoveRoomMemberAsync(Guid adminUserId, Guid roomId, Guid memberUserId, CancellationToken cancellationToken = default)
        {
            try
            {
                var adminUserRoom = await _context.UserRooms
                    .Include(ur => ur.Room)
                    .Where(ur => ur.RoomId == roomId && ur.UserId == adminUserId && ur.Room.Status == RoomStatus.Active)
                    .FirstOrDefaultAsync(cancellationToken);

                if (adminUserRoom == null)
                {
                    _logger.LogError("Active room '{RoomId}' not found or admin user '{UserId}' is not in that room", roomId, adminUserId);
                    return Result.Fail("Room not found or admin user is not in that room");
                }

                if (!adminUserRoom.IsAdmin)
                {
                    _logger.LogWarning("User '{UserId}' is not admin of room '{RoomId}'", adminUserId, roomId);
                    return Result.Fail("Only room admin can remove members");
                }

                if (adminUserId == memberUserId)
                {
                    _logger.LogWarning("Admin '{UserId}' attempted to remove themselves from room '{RoomId}'", adminUserId, roomId);
                    return Result.Fail("Admin cannot remove themselves from the room");
                }

                var memberUserRoom = await _context.UserRooms
                    .Where(ur => ur.RoomId == roomId && ur.UserId == memberUserId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (memberUserRoom == null)
                {
                    _logger.LogWarning("User '{MemberUserId}' not found in room '{RoomId}'", memberUserId, roomId);
                    return Result.Fail("Member not found in the room");
                }

                _context.UserRooms.Remove(memberUserRoom);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("User '{MemberUserId}' removed from room '{RoomId}' by admin '{AdminUserId}'", memberUserId, roomId, adminUserId);

                return Result.Ok("Member removed from the room successfully");
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "RemoveRoomMemberAsync was canceled for roomId {RoomId}", roomId);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while removing member from room '{RoomId}'", roomId);
                return Result.Fail("Unexpected error occurred while removing the member from the room");
            }
        }

        public async Task<Result<string>> EditRoomStatusAsync(Guid userId, Guid roomId, RoomStatus newStatus, CancellationToken cancellationToken = default)
        {
            try
            {
                var userRoom = await _context.UserRooms
                    .Include(ur => ur.Room)
                    .Where(ur => ur.RoomId == roomId && ur.UserId == userId && ur.Room.Status == RoomStatus.Active)
                    .FirstOrDefaultAsync(cancellationToken);

                if (userRoom == null)
                {
                    _logger.LogError("Active room '{RoomId}' not found or user '{UserId}' is not in that room", roomId, userId);
                    return Result.Fail("Room not found or user is not in that room");
                }

                if (!userRoom.IsAdmin)
                {
                    _logger.LogWarning("User '{UserId}' is not admin of room '{RoomId}'", userId, roomId);
                    return Result.Fail("Only room admin can change room status");
                }

                var room = userRoom.Room;

                if (room.Status == newStatus)
                {
                    _logger.LogInformation("Room '{RoomId}' already has status '{Status}'", roomId, newStatus);
                    return Result.Ok("Room status is already set to the specified value");
                }

                room.Status = newStatus;
                room.ModifiedAt = DateTime.UtcNow;
                room.ModifiedBy = userId;

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Room status updated for Room '{RoomId}' to '{Status}' by User '{UserId}'", roomId, newStatus, userId);

                return Result.Ok("Room status updated successfully");
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "EditRoomStatusAsync was canceled for roomId {RoomId}", roomId);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while editing room status for RoomId {RoomId}", roomId);
                return Result.Fail("Unexpected error occurred while editing the room status");
            }
        }
    }
}
