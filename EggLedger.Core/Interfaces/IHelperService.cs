using EggLedger.Core.Models;
using FluentResults;

namespace EggLedger.Core.Interfaces;

public interface IHelperService
{
    Task<Result<string>> GenerateOrderName(User user, int i, CancellationToken cancellationToken = default);
    Task<Result<string>> GenerateContainerName(User user, CancellationToken cancellationToken = default);
    int GenerateNewRoomCode();
}