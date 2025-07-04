using EggLedger.Models.Models;
using FluentResults;

namespace EggLedger.Services.Interfaces;

public interface IHelperService
{
    Task<Result<string>> GenerateOrderName(User user, int i, CancellationToken cancellationToken = default);
    Task<Result<string>> GenerateContainerName(User user, CancellationToken cancellationToken = default);
    int GenerateNewRoomCode();
}