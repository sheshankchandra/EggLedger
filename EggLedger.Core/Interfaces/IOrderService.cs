using EggLedger.Core.DTOs.Order;
using EggLedger.Core.Models;
using FluentResults;
using Microsoft.AspNetCore.Mvc;

namespace EggLedger.Core.Interfaces
{
    public interface IOrderService
    {
        Task<Result<string>> CreateStockOrderAsync(Guid userId, int roomCode, StockOrderDto dto);
        Task<Result<string>> CreateConsumeOrderAsync(Guid userId, int roomCode, ConsumeOrderDto dto);
        Task<Result<OrderDto>> GetOrderByIdAsync(Guid orderId);
        Task<Result<List<OrderDto>>> GetOrdersByUserAsync(Guid userId);
        Task<Result<List<OrderDto>>> GetOrdersByContainerAsync(Guid containerId);
        OrderDto MapToOrderDto(Order order);
    }
}
