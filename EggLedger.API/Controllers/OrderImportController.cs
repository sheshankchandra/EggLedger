using EggLedger.API.Services;
using EggLedger.Core.DTOs;
using EggLedger.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using EggLedger.Core.Models;

namespace EggLedger.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderImportController : ControllerBase
    {
        private readonly IOrderImportService _service;

        // ReSharper disable once ConvertToPrimaryConstructor
        public OrderImportController(IOrderImportService service)
        {
            _service = service;
        }

        [HttpPost("stock")]
        public async Task<IActionResult> StockOrder([FromBody] StockingOrderDto dto)
        {
            var result = await _service.StockOrderAsync(dto);
            return Ok(result);
        }           
        
        [HttpPost("consume")]
        public async Task<IActionResult> ConsumeOrder([FromBody] ConsumingOrderDto dto)
        {
            var result = await _service.ConsumeOrderAsync(dto);
            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }
    }
}
