using EggLedger.API.Services;
using EggLedger.Core.DTOs.Order;
using EggLedger.Core.Interfaces;
using EggLedger.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EggLedger.API.Controllers
{
    [Authorize(Policy = "RoomMember")]
    [Route("egg-ledger-api/{roomCode:int}/orders")]
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

        // POST: api/{roomCode}/orders/stock
        [HttpPost("stock")]
        public async Task<IActionResult> CreateStockOrder([FromRoute] int roomCode, [FromBody] StockOrderDto dto)
        {
            _logger.LogInformation("Received request to create Stocking order.");

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _orderService.CreateStockOrderAsync(userId, roomCode, dto);

            if (result is { IsSuccess: true, Value: not null })
            {
                _logger.LogInformation("Successfully created Stocked order: {OrderName}", result.Value);
                return Ok(result.Value);
            }

            _logger.LogWarning("Failed to stock order. Errors: {Errors}", string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(result.Errors.Select(e => e.Message));
        }

        // POST: api/{roomCode}/orders/consume
        [HttpPost("consume")]
        public async Task<IActionResult> CreateConsumeOrder([FromRoute] int roomCode, [FromBody] ConsumeOrderDto dto)
        {
            _logger.LogInformation("Received request to create Consuming order.");

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _orderService.CreateConsumeOrderAsync(userId, roomCode, dto);

            if (result is { IsSuccess: true, Value: not null })
            {
                _logger.LogInformation("Successfully created Consumed order: {OrderName}", result.Value);
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

        // GET: api/{roomCode}/orders/{orderId}
        [HttpGet("{orderId:guid}")]
        public async Task<IActionResult> GetOrder([FromRoute] int roomCode, [FromRoute] Guid orderId)
        {
            _logger.LogInformation("Received request to retrieve order information. Order ID : '{orderId}'", orderId);

            var result = await _orderService.GetOrderByIdAsync(orderId);

            if (result is { IsSuccess: true, Value: not null })
            {
                _logger.LogInformation("Successfully retrieved order information. Order ID : '{orderId}'", orderId);
                return Ok(result.Value);
            }

            _logger.LogError("Failed to retrieve order information. Order ID : '{orderId}'", orderId);
            return NotFound(result.Errors.Select(e => e.Message));
        }

        // GET: api/{roomCode}/orders/user/{userId}
        [HttpGet("user/{requestUserId:guid}")]
        public async Task<IActionResult> GetOrdersByUser([FromRoute] int roomCode, [FromRoute] Guid requestUserId)
        {
            _logger.LogInformation("Received request to retrieve a User '{userId}' Order information.", requestUserId);

            var result = await _orderService.GetOrdersByUserAsync(requestUserId);

            if (result is { IsSuccess: true, Value: not null })
            {
                _logger.LogInformation("Successfully retrieved a User '{userId}' Order information.", requestUserId);
                return Ok(result.Value);
            }

            return NotFound(result.Value);
        }

        // GET: api/{roomCode}/orders/container/{containerId}
        [HttpGet("container/{containerId:guid}")]
        public async Task<IActionResult> GetOrdersByContainer([FromRoute] int roomCode, [FromRoute] Guid containerId)
        {
            _logger.LogInformation("Received request to retrieve a Containers '{containerId}' Order information.", containerId);

            var result = await _orderService.GetOrdersByContainerAsync(containerId);

            if (result is { IsSuccess: true, Value: not null })
            {
                _logger.LogInformation("Successfully retrieve a Containers '{containerId}' Order information.", containerId);
                return Ok(result.Value);
            }

            return NotFound(result.Value);
        }
    }
}
