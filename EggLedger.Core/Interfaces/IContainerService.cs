using EggLedger.Core.DTOs;
using EggLedger.Core.DTOs.Container;
using EggLedger.Core.Models;

namespace EggLedger.Core.Interfaces;

public interface IContainerService
{
    Task<List<ContainerSummaryDto>> GetContainersAsync();
}