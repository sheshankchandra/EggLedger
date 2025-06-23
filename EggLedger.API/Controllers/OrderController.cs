using EggLedger.API.Services;
using EggLedger.Core.DTOs.Order;
using EggLedger.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EggLedger.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IOrderService orderService, ILogger<OrderController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        // POST: api/orderimport/stock
        [HttpPost("stock")]
        public async Task<IActionResult> StockOrder([FromBody] StockOrderDto dto)
        {
            _logger.LogInformation("Received request to create Stocking order.");
            var result = await _orderService.StockOrderAsync(dto);

            if (result.IsSuccess && result.Value != null)
            {
                _logger.LogInformation("Stocked order: {OrderName}", result.Value.OrderName);
                return Ok(result.Value);
            }

            _logger.LogWarning("Failed to stock order. Errors: {Errors}", string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(result.Errors.Select(e => e.Message));
        }

        // POST: api/orderimport/consume
        [HttpPost("consume")]
        public async Task<IActionResult> ConsumeOrder([FromBody] ConsumeOrderDto dto)
        {
            _logger.LogInformation("Received request to create Consuming order.");
            var result = await _orderService.ConsumeOrderAsync(dto);

            if (result.IsSuccess && result.Value != null)
            {
                _logger.LogInformation("Consumed order: {OrderName}", result.Value.OrderName);
                return Ok(result.Value);
            }

            if (result.Errors.Any(e => e.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("Consuming order not found.");
                return NotFound(result.Errors.Select(e => e.Message));
            }

            _logger.LogWarning("Failed to consume order. Errors: {Errors}", string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(result.Errors.Select(e => e.Message));
        }

        // GET: api/order/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(Guid id)
        {
            var result = await _orderService.GetOrderByIdAsync(id);
            if (result.IsSuccess && result.Value != null)
                return Ok(result.Value);
            return NotFound(result.Errors.Select(e => e.Message));
        }

        // GET: api/order/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetOrdersByUser(Guid userId)
        {
            var result = await _orderService.GetOrdersByUserAsync(userId);
            return Ok(result.Value); // Return empty list if none found
        }

        // GET: api/order/container/{containerId}
        [HttpGet("container/{containerId}")]
        public async Task<IActionResult> GetOrdersByContainer(Guid containerId)
        {
            var result = await _orderService.GetOrdersByContainerAsync(containerId);
            return Ok(result.Value); // Return empty list if none found
        }

        // GET: api/order/history/{userId}
        [HttpGet("history/{userId}")]
        public async Task<IActionResult> GetOrderHistory(Guid userId)
        {
            var result = await _orderService.GetOrderHistoryAsync(userId);
            return Ok(result.Value);
        }
    }
}
