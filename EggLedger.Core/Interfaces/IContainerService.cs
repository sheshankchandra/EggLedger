using EggLedger.Core.DTOs.Container;
using FluentResults;

namespace EggLedger.Core.Interfaces;

public interface IContainerService
{
    /// <summary>
    /// Retrieves all containers with summary information for a specific room.
    /// </summary>
    Task<Result<List<ContainerSummaryDto>>> GetAllContainersAsync(int roomCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single container by its ID with summary information.
    /// </summary>
    Task<Result<ContainerSummaryDto>> GetContainerAsync(Guid containerId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new container.
    /// </summary>
    Task<Result<ContainerSummaryDto>> CreateContainerAsync(int roomCode, ContainerCreateDto dto, CancellationToken cancellationToken = default);
    
    Task<Result<ContainerSummaryDto>> UpdateContainerAsync(Guid containerId, ContainerUpdateDto dto, CancellationToken cancellationToken = default);
    Task<Result> DeleteContainerAsync(Guid containerId, CancellationToken cancellationToken = default);
    Task<Result<List<ContainerSummaryDto>>> SearchContainersByOwnerNameAsync(int roomCode, string ownerName, CancellationToken cancellationToken = default);
    Task<Result<List<ContainerSummaryDto>>> GetPagedContainersAsync(int roomCode, int page, int pageSize, CancellationToken cancellationToken = default);
}