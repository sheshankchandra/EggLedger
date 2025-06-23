using EggLedger.Core.DTOs;
using EggLedger.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace EggLedger.Core.Interfaces
{
    public interface IOrderImportService
    {
        Task<ActionResult<Order>> StockOrderAsync(StockingOrderDto dto);
        Task<ActionResult<Order>> ConsumeOrderAsync(ConsumingOrderDto dto);
    }
}
