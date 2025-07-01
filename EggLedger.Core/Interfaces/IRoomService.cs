using EggLedger.Core.DTOs.Room;
using EggLedger.Core.DTOs.User;
using FluentResults;

namespace EggLedger.Core.Interfaces
{
    public interface IRoomService
    {
        Task<Result<int>> CreateRoomAsync(Guid userId, CreateRoomDto dto);
        Task<Result<int>> JoinRoomAsync(Guid userId, int roomCode);
        Task<Result<List<UserSummaryDto>>> GetAllRoomUsersAsync(int roomCode);
        Task<Result<string>> UpdateRoomPublicStatusAsync(UpdateRoomPublicStatusDto dto);
        Task<Result<List<RoomDto>>> GetAllUserRoomsAsync(Guid userId);
    }
}
