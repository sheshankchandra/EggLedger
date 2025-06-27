using EggLedger.Core.DTOs;
using EggLedger.Core.DTOs.Container;
using EggLedger.Core.Models;
using FluentResults;

namespace EggLedger.Core.Interfaces;

public interface IContainerService
{
    /// <summary>
    /// Retrieves all containers with summary information.
    /// </summary>
    Task<Result<List<ContainerSummaryDto>>> GetAllContainersAsync();

    /// <summary>
    /// Retrieves a single container by its ID with summary information.
    /// </summary>
    Task<Result<ContainerSummaryDto>> GetContainerAsync(Guid containerId);
    Task<Result<ContainerSummaryDto>> UpdateContainerAsync(Guid containerId, ContainerUpdateDto dto);
    Task<Result> DeleteContainerAsync(Guid containerId);
    Task<Result<List<ContainerSummaryDto>>> SearchContainersByOwnerNameAsync(string ownerName);
    Task<Result<List<ContainerSummaryDto>>> GetPagedContainersAsync(int page, int pageSize);
}