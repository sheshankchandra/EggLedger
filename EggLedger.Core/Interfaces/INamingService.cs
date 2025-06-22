using EggLedger.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace EggLedger.Core.Interfaces;

public interface INamingService
{
    Task<ActionResult<string>> GenerateOrderName(User user);
    Task<ActionResult<string>> GenerateContainerName(Guid userId);
}