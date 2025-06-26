using EggLedger.API.Data;
using EggLedger.Core.DTOs.Room;
using EggLedger.Core.Interfaces;
using EggLedger.Core.Models;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EggLedger.API.Services
{
    public class RoomService : IRoomService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderService> _logger;
        private readonly INamingService _namingService;

        public RoomService(ApplicationDbContext context, ILogger<OrderService> logger, INamingService namingService)
        {
            _context = context;
            _logger = logger;
            _namingService = namingService;
        }

        public async Task<Result<Room>> CreateRoomAsync(CreateRoomDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == dto.AdminUserId);

            if (user == null)
            {
                _logger.LogWarning("User not found, email '{Email}'", dto.AdminUserId);
                return Result.Fail("User not found");
            }

            var newRoom = new Room()
            {
                RoomId = Guid.NewGuid(),
                AdminUserId = user.UserId,
                RoomName = dto.RoomName,
                Code = _namingService.GenerateNewRoomCode(),
                IsOpen = dto.IsOpen,
                CreatedAt = DateTime.Now
            };

            _context.Rooms.Add(newRoom);
            await _context.SaveChangesAsync();

            _logger.LogInformation("New Room {Room.RooName} Created: {Room.RoomId}", newRoom.RoomName, newRoom.RoomId);

            return newRoom;
        }

        public async Task<Result<User>> JoinRoomAsync(JoinRoomDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == dto.UserId);

            if (user == null)
            {
                _logger.LogWarning("User not found, email '{Email}'", dto.UserId);
                return Result.Fail("User not found");
            }

            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.Code == dto.RoomCode);

            if (room == null)
            {
                _logger.LogWarning("Room not found, code '{Room.RoomCode}'", dto.RoomCode);
                return Result.Fail("Room not found");
            }

            if (!room.IsOpen)
            {
                _logger.LogWarning("Room is not Open, code '{Room.RoomCode}'", dto.RoomCode);
                return Result.Fail("Room is not Open");
            }

            room.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {user.Name} successfully joined Room {room.RoomName}", user.Name, room.RoomName);

            return Result.Ok(user);
        }
    }
}
