using EggLedger.DTO.Activity;
using FluentResults;

namespace EggLedger.Services.Interfaces;

public interface IActivityService
{
    Task<Result<List<ActivityEventDto>>> GetRoomActivityAsync(int roomCode, int page = 1, int pageSize = 30, CancellationToken cancellationToken = default);
}
