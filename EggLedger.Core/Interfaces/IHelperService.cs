using EggLedger.Core.Models;
using FluentResults;

namespace EggLedger.Core.Interfaces;

public interface IHelperService
{
    Task<Result<string>> GenerateOrderName(User user, int i);
    Task<Result<string>> GenerateContainerName(User user);
    int GenerateNewRoomCode();
}