using EggLedger.API.Services;
using EggLedger.Core.DTOs;
using EggLedger.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

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
        public async Task<IActionResult> AddEggTransaction([FromBody] EggTransactionDto dto)
        {
            var result = await _service.AddTransactionAsync(dto);
            return Ok(result);
        }
    }
}
