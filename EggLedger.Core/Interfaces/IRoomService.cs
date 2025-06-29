using EggLedger.Core.DTOs.Room;
using EggLedger.Core.DTOs.User;
using EggLedger.Core.Models;
using FluentResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EggLedger.Core.Interfaces
{
    public interface IRoomService
    {
        Task<Result<string>> CreateRoomAsync(Guid userId, CreateRoomDto dto);
        Task<Result<Room>> JoinRoomAsync(JoinRoomDto dto);
        Task<Result<List<UserSummaryDto>>> GetAllRoomUsersAsync(int roomCode);
        Task<Result<string>> UpdateRoomPublicStatusAsync(UpdateRoomPublicStatusDto dto);
        Task<Result<List<RoomDto>>> GetAllUserRoomsAsync(Guid userId);
    }
}
