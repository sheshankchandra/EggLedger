using EggLedger.Core.DTOs;
using EggLedger.Core.DTOs.Container;
using EggLedger.Core.Models;
using FluentResults;

namespace EggLedger.Core.Interfaces;

public interface IContainerService
{
    /// <summary>
    /// Retrieves all containers with summary information for a specific room.
    /// </summary>
    Task<Result<List<ContainerSummaryDto>>> GetAllContainersAsync(int roomCode);

    /// <summary>
    /// Retrieves a single container by its ID with summary information.
    /// </summary>
    Task<Result<ContainerSummaryDto>> GetContainerAsync(Guid containerId);
    
    /// <summary>
    /// Creates a new container.
    /// </summary>
    Task<Result<ContainerSummaryDto>> CreateContainerAsync(int roomCode, ContainerCreateDto dto);
    
    Task<Result<ContainerSummaryDto>> UpdateContainerAsync(Guid containerId, ContainerUpdateDto dto);
    Task<Result> DeleteContainerAsync(Guid containerId);
    Task<Result<List<ContainerSummaryDto>>> SearchContainersByOwnerNameAsync(int roomCode, string ownerName);
    Task<Result<List<ContainerSummaryDto>>> GetPagedContainersAsync(int roomCode, int page, int pageSize);
}