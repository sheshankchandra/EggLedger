using EggLedger.Core.DTOs.Order;
using EggLedger.Core.Models;
using FluentResults;
using Microsoft.AspNetCore.Mvc;

namespace EggLedger.Core.Interfaces
{
    public interface IOrderService
    {
        Task<Result<Order>> StockOrderAsync(StockOrderDto dto);
        Task<Result<Order>> ConsumeOrderAsync(ConsumeOrderDto dto);
        Task<Result<OrderDto>> GetOrderByIdAsync(Guid orderId);
        Task<Result<List<OrderDto>>> GetOrdersByUserAsync(Guid userId);
        Task<Result<List<OrderDto>>> GetOrdersByContainerAsync(Guid containerId);
        Task<Result<List<OrderDto>>> GetOrderHistoryAsync(Guid userId);
        OrderDto MapToOrderDto(Order order);
    }
}
