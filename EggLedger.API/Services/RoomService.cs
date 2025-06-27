using EggLedger.API.Data;
using EggLedger.Core.DTOs.Room;
using EggLedger.Core.Interfaces;
using EggLedger.Core.Models;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EggLedger.API.Services
{
    public class RoomService : IRoomService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderService> _logger;
        private readonly IHelperService _helperService;

        public RoomService(ApplicationDbContext context, ILogger<OrderService> logger, IHelperService helperService)
        {
            _context = context;
            _logger = logger;
            _helperService = helperService;
        }

        public async Task<Result<Room>> CreateRoomAsync(CreateRoomDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == dto.CreatorUserId);

            if (user == null)
            {
                _logger.LogWarning("User not found, email '{Email}'", dto.CreatorUserId);
                return Result.Fail("User not found");
            }

            var room = new Room()
            {
                RoomId = Guid.NewGuid(),
                RoomName = dto.RoomName,
                RoomCode = _helperService.GenerateNewRoomCode(),
                IsPublic = dto.IsOpen,
                CreatedAt = _helperService.GetIndianTime()
            };

            var userRoom = new UserRoom
            {
                RoomId = room.RoomId,
                UserId = dto.CreatorUserId,
                IsAdmin = true,
                JoinedAt = _helperService.GetIndianTime()
            };

            _context.Rooms.Add(room);
            _context.UserRooms.Add(userRoom);
            await _context.SaveChangesAsync();

            _logger.LogInformation("New Room {Room.RoomName} Created: {Room.RoomId}", room.RoomName, room.RoomId);

            return room;
        }

        public async Task<Result<Room>> JoinRoomAsync(JoinRoomDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == dto.UserId);

            if (user == null)
            {
                _logger.LogWarning("User not found, email '{Email}'", dto.UserId);
                return Result.Fail("User not found");
            }

            var room = await _context.Rooms.Include(room => room.UserRooms)
                                           .FirstOrDefaultAsync(r => r.RoomCode == dto.RoomCode);

            if (room == null)
            {
                _logger.LogWarning("Room not found, code '{Room.RoomCode}'", dto.RoomCode);
                return Result.Fail("Room not found");
            }

            if (room.UserRooms.Any(ur => ur.UserId == user.UserId))
            {
                _logger.LogError("User : {user.FirstName} already in room : {room.RoomName}", user.FirstName, room.RoomName);
                return Result.Fail("User already in room");
            }

            if (!room.IsPublic)
            {
                _logger.LogWarning("Room is not Public, code '{Room.RoomCode}'", dto.RoomCode);
                return Result.Fail("Room is not Public");
            }

            var userRoom = new UserRoom
            {
                RoomId = room.RoomId,
                UserId = dto.UserId,
                IsAdmin = false,
                JoinedAt = _helperService.GetIndianTime()
            };

            _context.UserRooms.Add(userRoom);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {user.Name} successfully joined Room {room.RoomName}", user.Name, room.RoomName);

            return Result.Ok(room);
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
    }
}
