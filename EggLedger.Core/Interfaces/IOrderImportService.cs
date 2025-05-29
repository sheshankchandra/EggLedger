using EggLedger.Core.DTOs;
using EggLedger.Core.Models;

namespace EggLedger.Core.Interfaces
{
    public interface IOrderImportService
    {
        Task<Order> StockOrderAsync(StockingOrderDto dto);
        Task<Order> ConsumeOrderAsync(ConsumingOrderDto dto);
    }
}
