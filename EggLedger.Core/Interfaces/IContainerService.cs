using EggLedger.Core.DTOs;
using EggLedger.Core.DTOs.Container;
using EggLedger.Core.Models;
using FluentResults;

namespace EggLedger.Core.Interfaces;

public interface IContainerService
{
    Task<Result<List<ContainerSummaryDto>>> GetAllContainersAsync();
    Task<Result<ContainerSummaryDto>> GetContainerAsync(Guid containerId);
}