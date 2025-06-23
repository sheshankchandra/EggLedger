using EggLedger.Core.Models;
using FluentResults;
using Microsoft.AspNetCore.Mvc;

namespace EggLedger.Core.Interfaces;

public interface INamingService
{
    Task<Result<string>> GenerateOrderName(User user, int i);
    Task<Result<string>> GenerateContainerName(User user);
}