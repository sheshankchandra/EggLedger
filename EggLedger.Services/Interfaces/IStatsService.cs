using EggLedger.DTO.Stats;
using EggLedger.Models.Enums;
using FluentResults;

namespace EggLedger.Services.Interfaces;

public interface IStatsService
{
    Task<Result<UserStatsDto>> GetUserStatsAsync(Guid userId, StatsRange range, CancellationToken cancellationToken = default);
}
