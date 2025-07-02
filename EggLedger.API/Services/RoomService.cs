using EggLedger.API.Data;
using EggLedger.Core.DTOs.Room;
using EggLedger.Core.DTOs.User;
using EggLedger.Core.Interfaces;
using EggLedger.Core.Models;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace EggLedger.API.Services
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

        public async Task<Result<int>> CreateRoomAsync(Guid userId, CreateRoomDto dto)
        {
            var room = new Room()
            {
                RoomId = Guid.NewGuid(),
                RoomName = dto.RoomName,
                RoomCode = _helperService.GenerateNewRoomCode(),
                IsPublic = dto.IsOpen,
                CreatedAt = DateTime.UtcNow
            };

            var userRoom = new UserRoom
            {
                RoomId = room.RoomId,
                UserId = userId,
                IsAdmin = true,
                JoinedAt = DateTime.UtcNow
            };

            _context.Rooms.Add(room);
            _context.UserRooms.Add(userRoom);
            await _context.SaveChangesAsync();

            _logger.LogInformation("New Room {Room.RoomName} Created: {Room.RoomId}", room.RoomName, room.RoomId);

            return Result.Ok(room.RoomCode);
        }

        public async Task<Result<int>> JoinRoomAsync(Guid userId, int roomCode)
        {
            var room = await _context.Rooms.Include(room => room.UserRooms)
                                           .FirstOrDefaultAsync(r => r.RoomCode == roomCode);

            if (room == null)
            {
                _logger.LogWarning("Room not found, code '{Room.RoomCode}'", roomCode);
                return Result.Fail("Room not found");
            }

            if (room.UserRooms.Any(ur => ur.UserId == userId))
            {
                _logger.LogError("User : {userId} already in room : {room.RoomName}", userId, room.RoomName);
                return Result.Fail("User already in room");
            }

            if (!room.IsPublic)
            {
                _logger.LogWarning("Room is not Public, code '{Room.RoomCode}'", roomCode);
                return Result.Fail("Room is not Public");
            }

            var userRoom = new UserRoom
            {
                RoomId = room.RoomId,
                UserId = userId,
                IsAdmin = false,
                JoinedAt = DateTime.UtcNow
            };

            _context.UserRooms.Add(userRoom);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {userId} successfully joined Room {room.RoomName}", userId, room.RoomName);

            return Result.Ok(roomCode);
        }

        public async Task<Result<List<UserSummaryDto>>> GetAllRoomUsersAsync(int roomCode)
        {
            var room = await _context.Rooms.Where(r => r.RoomCode == roomCode).FirstOrDefaultAsync();

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
                .ToListAsync();

            return Result.Ok(users);
        }

        public async Task<Result<string>> UpdateRoomPublicStatusAsync(UpdateRoomPublicStatusDto dto)
        {
            try
            {
                UserRoom? userRoom = await _context.UserRooms
                    .Include(ur => ur.Room)
                    .FirstOrDefaultAsync(ur => ur.RoomId == dto.RoomId && ur.UserId == dto.UserId);

                if (userRoom == null)
                {
                    _logger.LogError("Room '{RoomId}' not found or user '{UserId}' is not in that room", dto.RoomId, dto.UserId);
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

                _logger.LogInformation("Updated room '{RoomName}' visibility to {IsPublic}", room.RoomName, room.IsPublic);

                await _context.SaveChangesAsync();
                return Result.Ok("Room visibility updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating room visibility for RoomId {RoomId}", dto.RoomId);
                return Result.Fail("Unexpected error occurred while updating the room's public status");
            }
        }

        public async Task<Result<List<RoomDto>>> GetAllUserRoomsAsync(Guid userId)
        {
            try
            {
                var userRooms = await _context.UserRooms
                    .AsNoTracking()
                    .Include(ur => ur.Room)
                    .Where(ur => ur.UserId == userId)
                    .Select(ur => new RoomDto
                    {
                        RoomId = ur.Room.RoomId,
                        RoomName = ur.Room.RoomName,
                        RoomCode = ur.Room.RoomCode,
                        IsOpen = ur.Room.IsPublic,
                        AdminUserId = ur.IsAdmin ? userId : null,
                        CreateAt = ur.Room.CreatedAt,
                        ContainerCount = _context.Containers.Count(c => c.RoomId == ur.Room.RoomId),
                        TotalEggs = _context.Containers
                            .Where(c => c.RoomId == ur.Room.RoomId)
                            .SelectMany(c => _context.OrderDetails.Where(od => od.ContainerId == c.ContainerId))
                            .Sum(od => (int?)od.DetailQuantity) ?? 0,
                        MemberCount = _context.UserRooms.Count(ur2 => ur2.RoomId == ur.Room.RoomId)
                    })
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} rooms for user {UserId}", userRooms.Count, userId);
                return Result.Ok(userRooms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting rooms for user {UserId}", userId);
                return Result.Fail("Failed to retrieve user rooms");
            }
        }

        public async Task<Result<RoomDto>> GetRoomByCodeAsync(int roomCode)
        {
            try
            {
                var room = await _context.Rooms
                    .AsNoTracking()
                    .Where(r => r.RoomCode == roomCode)
                    .Include(r => r.UserRooms)
                    .Select(r => new RoomDto
                    {
                        RoomId = r.RoomId,
                        RoomName = r.RoomName,
                        RoomCode = r.RoomCode,
                        IsOpen = r.IsPublic,
                        AdminUserId = r.UserRooms.Where(ur => ur.IsAdmin).Select(ur => ur.UserId).FirstOrDefault(),
                        CreateAt = r.CreatedAt,
                        ContainerCount = _context.Containers.Count(c => c.RoomId == r.RoomId),
                        TotalEggs = _context.Containers
                            .Where(c => c.RoomId == r.RoomId)
                            .SelectMany(c => _context.OrderDetails.Where(od => od.ContainerId == c.ContainerId))
                            .Sum(od => (int?)od.DetailQuantity) ?? 0,
                        MemberCount = _context.UserRooms.Count(ur2 => ur2.RoomId == r.RoomId)
                    })
                    .FirstOrDefaultAsync();

                if (room == null)
                {
                    _logger.LogWarning("Room with code {roomCode} not found", roomCode);
                    return Result.Fail<RoomDto>("Room not found");
                }

                _logger.LogInformation("Retrieved room {roomCode}", roomCode);
                return Result.Ok(room);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving room {roomCode}", roomCode);
                return Result.Fail("Failed to retrieve room");
            }
        }
    }
}
