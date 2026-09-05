using EggLedger.DTO.Room;
using EggLedger.DTO.User;
using EggLedger.Models.Enums;
using FluentResults;

namespace EggLedger.Services.Interfaces;

public interface IRoomService
{
    Task<Result<int>> CreateRoomAsync(Guid userId, CreateRoomDto dto, CancellationToken cancellationToken = default);
    Task<Result<JoinRoomResultDto>> JoinRoomAsync(Guid userId, int roomCode, CancellationToken cancellationToken = default);
    Task<Result<List<UserSummaryDto>>> GetAllRoomUsersAsync(int roomCode, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<Result<string>> UpdateRoomPublicStatusAsync(UpdateRoomPublicStatusDto dto, CancellationToken cancellationToken = default);
    Task<Result<List<RoomDto>>> GetAllUserRoomsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<RoomDto>> GetRoomByCodeAsync(int roomCode, CancellationToken cancellationToken = default);
    Task<Result<int>> DeleteRoomAsync(int roomCode, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<string>> EditRoomNameAsync(Guid userId, Guid roomId, string newRoomName, CancellationToken cancellationToken = default);
    Task<Result<string>> RemoveRoomMemberAsync(Guid adminUserId, Guid roomId, Guid memberUserId, CancellationToken cancellationToken = default);
    Task<Result<string>> EditRoomStatusAsync(Guid userId, Guid roomId, RoomStatus newStatus, CancellationToken cancellationToken = default);
    Task<Result<List<PendingMemberDto>>> GetPendingMembersAsync(Guid adminUserId, int roomCode, CancellationToken cancellationToken = default);
    Task<Result<string>> ApproveMemberAsync(Guid adminUserId, int roomCode, Guid memberUserId, CancellationToken cancellationToken = default);
    Task<Result<string>> RejectMemberAsync(Guid adminUserId, int roomCode, Guid memberUserId, CancellationToken cancellationToken = default);

}
