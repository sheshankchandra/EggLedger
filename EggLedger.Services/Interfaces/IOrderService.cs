using EggLedger.DTO.Order;
using EggLedger.Models.Models;
using FluentResults;

namespace EggLedger.Services.Interfaces
{
    public interface IOrderService
    {
        Task<Result<string>> CreateStockOrderAsync(Guid userId, int roomCode, StockOrderDto dto, CancellationToken cancellationToken = default);
        Task<Result<string>> CreateConsumeOrderAsync(Guid userId, int roomCode, ConsumeOrderDto dto, CancellationToken cancellationToken = default);
        Task<Result<OrderDto>> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default);
        Task<Result<List<OrderDto>>> GetOrdersByUserAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<Result<List<OrderDto>>> GetOrdersByContainerAsync(Guid containerId, CancellationToken cancellationToken = default);
        OrderDto MapToOrderDto(Order order);
    }
}
