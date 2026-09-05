using EggLedger.Data;
using EggLedger.DTO.Room;
using EggLedger.DTO.User;
using EggLedger.Models.Enums;
using EggLedger.Models.Models;
using EggLedger.Services.Errors;
using EggLedger.Services.Extensions;
using EggLedger.Services.Interfaces;
using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EggLedger.Services.Services;

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
        return await _logger.ExecuteAsync(async () =>
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
        }, "An error occurred while creating the room.");
    }

    public async Task<Result<JoinRoomResultDto>> JoinRoomAsync(Guid userId, int roomCode, CancellationToken cancellationToken = default)
    {
        return await _logger.ExecuteAsync(async () =>
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

            var existing = room.UserRooms.FirstOrDefault(ur => ur.UserId == userId);
            if (existing != null)
            {
                _logger.LogWarning("User : {UserId} already in room : {RoomName}", userId, room.RoomName);
                return existing.Status == UserRoomStatus.Pending
                    ? Result.Fail("Your request to join this room is already pending approval")
                    : Result.Fail("User already in room");
            }

            // Knowing the room code is enough to request joining, for both Private and Public
            // rooms - but a Private room's request needs the admin to approve it before the new
            // member gets any actual access (RoomMemberHandler only counts Approved rows).
            var isPending = !room.IsPublic;

            var userRoom = new UserRoom
            {
                Id = Guid.NewGuid(),
                RoomId = room.RoomId,
                UserId = userId,
                IsAdmin = false,
                JoinedAt = DateTime.UtcNow,
                Status = isPending ? UserRoomStatus.Pending : UserRoomStatus.Approved,
            };

            _context.UserRooms.Add(userRoom);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} {Verb} Room {RoomName}", userId, isPending ? "requested to join" : "successfully joined", room.RoomName);

            return Result.Ok(new JoinRoomResultDto { RoomCode = roomCode, IsPending = isPending });
        }, "An error occurred while joining the room.");
    }

    public async Task<Result<List<UserSummaryDto>>> GetAllRoomUsersAsync(int roomCode, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        return await _logger.ExecuteAsync(async () =>
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var room = await _context.Rooms
                .Where(r => r.RoomCode == roomCode && r.Status == RoomStatus.Active)
                .FirstOrDefaultAsync(cancellationToken);

            if (room == null)
            {
                return Result.Fail("Room not found");
            }

            var users = await _context.Users.AsNoTracking()
                .Include(u => u.UserRooms)
                .Where(u => u.UserRooms.Any(ur => ur.RoomId == room.RoomId && ur.Status == UserRoomStatus.Approved))
                .OrderBy(u => u.FirstName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserSummaryDto
                {
                    UserId = u.UserId,
                    Name = u.Name,
                    Email = u.Email,
                    Role = u.Role
                })
                .ToListAsync(cancellationToken);

            return Result.Ok(users);
        }, "An error occurred while retrieving room users.");
    }

    public async Task<Result<string>> UpdateRoomPublicStatusAsync(UpdateRoomPublicStatusDto dto, CancellationToken cancellationToken = default)
    {
        return await _logger.ExecuteAsync(async () =>
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
        }, "Unexpected error occurred while updating the room's public status");
    }

    public async Task<Result<List<RoomDto>>> GetAllUserRoomsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _logger.ExecuteAsync(async () =>
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
                    IsPending = ur.Status == UserRoomStatus.Pending,
                    // A pending request has no visibility into room contents yet.
                    ContainerCount = ur.Status == UserRoomStatus.Pending ? 0 : _context.Containers.Count(c =>
                        c.RoomId == ur.Room.RoomId &&
                        c.Status == ContainerStatus.Available &&
                        c.RemainingQuantity > 0),
                    TotalEggs = ur.Status == UserRoomStatus.Pending ? 0 : _context.Containers
                        .Where(c =>
                            c.RoomId == ur.Room.RoomId &&
                            c.Status == ContainerStatus.Available &&
                            c.RemainingQuantity > 0)
                        .Sum(c => c.RemainingQuantity),
                    MemberCount = _context.UserRooms.Count(ur2 => ur2.RoomId == ur.Room.RoomId && ur2.Status == UserRoomStatus.Approved)
                })
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Retrieved {Count} active rooms for user {UserId}", userRooms.Count, userId);
            return Result.Ok(userRooms);
        }, "Failed to retrieve user rooms");
    }

    public async Task<Result<RoomDto>> GetRoomByCodeAsync(int roomCode, CancellationToken cancellationToken = default)
    {
        return await _logger.ExecuteAsync(async () =>
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
                    ContainerCount = _context.Containers.Count(c =>
                        c.RoomId == r.RoomId &&
                        c.Status == ContainerStatus.Available &&
                        c.RemainingQuantity > 0),
                    TotalEggs = _context.Containers
                        .Where(c =>
                            c.RoomId == r.RoomId &&
                            c.Status == ContainerStatus.Available &&
                            c.RemainingQuantity > 0)
                        .Sum(c => c.RemainingQuantity),
                    MemberCount = _context.UserRooms.Count(ur2 => ur2.RoomId == r.RoomId && ur2.Status == UserRoomStatus.Approved)
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (room == null)
            {
                _logger.LogWarning("Active room with code {RoomCode} not found", roomCode);
                return Result.Fail<RoomDto>("Room not found");
            }

            _logger.LogInformation("Retrieved room {RoomCode}", roomCode);
            return Result.Ok(room);
        }, "Failed to retrieve room");
    }

    public async Task<Result<int>> DeleteRoomAsync(int roomCode, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _logger.ExecuteAsync(async () =>
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
                                    od.OrderDetailStatus == OrderDetailStatus.Pending)
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

            var archiveTime = DateTime.UtcNow;

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

            var userRoomsToRemove = await _context.UserRooms
                .Where(ur => ur.RoomId == room.RoomId)
                .ToListAsync(cancellationToken);
            _context.UserRooms.RemoveRange(userRoomsToRemove);

            room.Status = RoomStatus.Archived;
            room.DeletedAt = archiveTime;
            room.DeletedBy = userId;
            room.DeletionReason = "Room archived by admin";
            room.ModifiedAt = archiveTime;
            room.ModifiedBy = userId;

            // A single SaveChanges is atomic on its own and stays compatible with the
            // retrying execution strategy, which forbids user-initiated transactions.
            await _context.SaveChangesAsync(cancellationToken);

            var totalAffectedRows = containersToArchive.Count + userRoomsToRemove.Count + 1;

            _logger.LogInformation("Successfully archived room {RoomCode}. " +
                "Containers archived: {ContainersArchived}, UserRooms removed: {UserRoomsDeleted}",
                roomCode, containersToArchive.Count, userRoomsToRemove.Count);

            return Result.Ok(totalAffectedRows);
        }, "An error occurred while archiving the room");
    }

    public async Task<Result<string>> EditRoomNameAsync(Guid userId, Guid roomId, string newRoomName, CancellationToken cancellationToken = default)
    {
        return await _logger.ExecuteAsync(async () =>
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
        }, "Unexpected error occurred while editing the room name");
    }

    public async Task<Result<string>> RemoveRoomMemberAsync(Guid adminUserId, Guid roomId, Guid memberUserId, CancellationToken cancellationToken = default)
    {
        return await _logger.ExecuteAsync(async () =>
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
        }, "Unexpected error occurred while removing the member from the room");
    }

    public async Task<Result<string>> EditRoomStatusAsync(Guid userId, Guid roomId, RoomStatus newStatus, CancellationToken cancellationToken = default)
    {
        return await _logger.ExecuteAsync(async () =>
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
        }, "Unexpected error occurred while editing the room status");
    }

    public async Task<Result<List<PendingMemberDto>>> GetPendingMembersAsync(Guid adminUserId, int roomCode, CancellationToken cancellationToken = default)
    {
        return await _logger.ExecuteAsync(async () =>
        {
            var room = await _context.Rooms
                .Where(r => r.RoomCode == roomCode && r.Status == RoomStatus.Active)
                .FirstOrDefaultAsync(cancellationToken);
            if (room == null)
                return Result.Fail(new NotFoundError("Room not found"));

            var isAdmin = await _context.UserRooms.AnyAsync(
                ur => ur.RoomId == room.RoomId && ur.UserId == adminUserId && ur.IsAdmin && ur.Status == UserRoomStatus.Approved,
                cancellationToken);
            if (!isAdmin)
                return Result.Fail("Only room admin can view pending requests");

            var pending = await _context.UserRooms
                .AsNoTracking()
                .Where(ur => ur.RoomId == room.RoomId && ur.Status == UserRoomStatus.Pending)
                .Include(ur => ur.User)
                .OrderBy(ur => ur.JoinedAt)
                .ToListAsync(cancellationToken);

            var dtos = pending.Select(ur => new PendingMemberDto
            {
                UserId = ur.UserId,
                Name = ur.User.Name,
                Email = ur.User.Email,
                RequestedAt = ur.JoinedAt,
            }).ToList();

            return Result.Ok(dtos);
        }, "An error occurred while retrieving pending join requests.");
    }

    public async Task<Result<string>> ApproveMemberAsync(Guid adminUserId, int roomCode, Guid memberUserId, CancellationToken cancellationToken = default)
    {
        return await _logger.ExecuteAsync(async () =>
        {
            var pendingRow = await GetPendingRowForAdminActionAsync(adminUserId, roomCode, memberUserId, cancellationToken);
            if (pendingRow.IsFailed)
                return pendingRow.ToResult();

            pendingRow.Value.Status = UserRoomStatus.Approved;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User '{MemberUserId}' approved into room code '{RoomCode}' by admin '{AdminUserId}'", memberUserId, roomCode, adminUserId);
            return Result.Ok("Member approved");
        }, "An error occurred while approving the member.");
    }

    public async Task<Result<string>> RejectMemberAsync(Guid adminUserId, int roomCode, Guid memberUserId, CancellationToken cancellationToken = default)
    {
        return await _logger.ExecuteAsync(async () =>
        {
            var pendingRow = await GetPendingRowForAdminActionAsync(adminUserId, roomCode, memberUserId, cancellationToken);
            if (pendingRow.IsFailed)
                return pendingRow.ToResult();

            // Deleted rather than flagged Rejected, so the composite (UserId, RoomId) unique
            // index doesn't permanently block the same user from requesting to join again.
            _context.UserRooms.Remove(pendingRow.Value);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User '{MemberUserId}' rejected from room code '{RoomCode}' by admin '{AdminUserId}'", memberUserId, roomCode, adminUserId);
            return Result.Ok("Member rejected");
        }, "An error occurred while rejecting the member.");
    }

    private async Task<Result<UserRoom>> GetPendingRowForAdminActionAsync(Guid adminUserId, int roomCode, Guid memberUserId, CancellationToken cancellationToken)
    {
        var room = await _context.Rooms
            .Where(r => r.RoomCode == roomCode && r.Status == RoomStatus.Active)
            .FirstOrDefaultAsync(cancellationToken);
        if (room == null)
            return Result.Fail(new NotFoundError("Room not found"));

        var isAdmin = await _context.UserRooms.AnyAsync(
            ur => ur.RoomId == room.RoomId && ur.UserId == adminUserId && ur.IsAdmin && ur.Status == UserRoomStatus.Approved,
            cancellationToken);
        if (!isAdmin)
            return Result.Fail("Only room admin can manage join requests");

        var pendingRow = await _context.UserRooms
            .FirstOrDefaultAsync(ur => ur.RoomId == room.RoomId && ur.UserId == memberUserId && ur.Status == UserRoomStatus.Pending, cancellationToken);
        if (pendingRow == null)
            return Result.Fail(new NotFoundError("No pending request found for that user"));

        return Result.Ok(pendingRow);
    }
}
