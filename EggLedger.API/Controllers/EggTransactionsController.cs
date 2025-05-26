using EggLedger.API.Services;
using EggLedger.Core.DTOs;
using EggLedger.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EggLedger.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EggTransactionsController : ControllerBase
    {
        private readonly IEggTransactionService _service;

        // ReSharper disable once ConvertToPrimaryConstructor
        public EggTransactionsController(IEggTransactionService service)
        {
            _service = service;
        }

        [HttpPost("add")]
        public async Task<IActionResult> StockEggTransaction([FromBody] EggTransactionDto dto)
        {
            var result = await _service.StockEggTransactionAsync(dto);
            return Ok(result);
        }
        
        [HttpPost("consume")]
        public async Task<IActionResult> ConsumeEggTransaction([FromBody] EggTransactionDto dto)
        {
            var result = await _service.ConsumeEggTransactionAsync(dto);
            return Ok(result);
        }
    }
}
