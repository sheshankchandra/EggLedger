using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using EggLedger.DTO.Order;
using EggLedger.Services.Interfaces;
using Microsoft.Extensions.Logging;

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

        // POST: egg-ledger-api/{roomCode}/orders/stock
        [HttpPost("stock")]
        public async Task<IActionResult> CreateStockOrder([FromRoute] int roomCode, [FromBody] StockOrderDto dto, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Received request to create Stocking order.");

                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
                var result = await _orderService.CreateStockOrderAsync(userId, roomCode, dto, cancellationToken);

                if (result is { IsSuccess: true, Value: not null })
                {
                    _logger.LogInformation("Successfully created Stocked order: {OrderName}", result.Value);
                    return Ok(result.Value);
                }

                _logger.LogWarning("Failed to stock order. Errors: {Errors}", string.Join(", ", result.Errors.Select(e => e.Message)));
                return BadRequest(result.Errors.Select(e => e.Message));
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Request was canceled by the client for CreateStockOrder, roomCode: {RoomCode}", roomCode);
                return StatusCode(499, "Client closed request.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in CreateStockOrder for roomCode: {RoomCode}", roomCode);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        // POST: egg-ledger-api/{roomCode}/orders/consume
        [HttpPost("consume")]
        public async Task<IActionResult> CreateConsumeOrder([FromRoute] int roomCode, [FromBody] ConsumeOrderDto dto, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Received request to create Consuming order.");

                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
                var result = await _orderService.CreateConsumeOrderAsync(userId, roomCode, dto, cancellationToken);

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
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Request was canceled by the client for CreateConsumeOrder, roomCode: {RoomCode}", roomCode);
                return StatusCode(499, "Client closed request.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in CreateConsumeOrder for roomCode: {RoomCode}", roomCode);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        // GET: egg-ledger-api/{roomCode}/orders/{orderId}
        [HttpGet("{orderId:guid}")]
        public async Task<IActionResult> GetOrder([FromRoute] int roomCode, [FromRoute] Guid orderId, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Received request to retrieve order information. Order ID : '{OrderId}'", orderId);

                var result = await _orderService.GetOrderByIdAsync(orderId, cancellationToken);

                if (result is { IsSuccess: true, Value: not null })
                {
                    _logger.LogInformation("Successfully retrieved order information. Order ID : '{OrderId}'", orderId);
                    return Ok(result.Value);
                }

                _logger.LogError("Failed to retrieve order information. Order ID : '{OrderId}'", orderId);
                return NotFound(result.Errors.Select(e => e.Message));
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Request was canceled by the client for GetOrder, roomCode: {RoomCode}, orderId: {OrderId}", roomCode, orderId);
                return StatusCode(499, "Client closed request.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in GetOrder for roomCode: {RoomCode}, orderId: {OrderId}", roomCode, orderId);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        // GET: egg-ledger-api/{roomCode}/orders/user/{userId}
        [HttpGet("user/{requestUserId:guid}")]
        public async Task<IActionResult> GetOrdersByUser([FromRoute] int roomCode, [FromRoute] Guid requestUserId, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Received request to retrieve a User '{UserId}' Order information.", requestUserId);

                var result = await _orderService.GetOrdersByUserAsync(requestUserId, cancellationToken);

                if (result is { IsSuccess: true, Value: not null })
                {
                    _logger.LogInformation("Successfully retrieved a User '{UserId}' Order information.", requestUserId);
                    return Ok(result.Value);
                }

                return NotFound(result.Value);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Request was canceled by the client for GetOrdersByUser, roomCode: {RoomCode}, userId: {UserId}", roomCode, requestUserId);
                return StatusCode(499, "Client closed request.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in GetOrdersByUser for roomCode: {RoomCode}, userId: {UserId}", roomCode, requestUserId);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        // GET: egg-ledger-api/{roomCode}/orders/container/{containerId}
        [HttpGet("container/{containerId:guid}")]
        public async Task<IActionResult> GetOrdersByContainer([FromRoute] int roomCode, [FromRoute] Guid containerId, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Received request to retrieve a Containers '{ContainerId}' Order information.", containerId);

                var result = await _orderService.GetOrdersByContainerAsync(containerId, cancellationToken);

                if (result is { IsSuccess: true, Value: not null })
                {
                    _logger.LogInformation("Successfully retrieve a Containers '{ContainerId}' Order information.", containerId);
                    return Ok(result.Value);
                }

                return NotFound(result.Value);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Request was canceled by the client for GetOrdersByContainer, roomCode: {RoomCode}, containerId: {ContainerId}", roomCode, containerId);
                return StatusCode(499, "Client closed request.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in GetOrdersByContainer for roomCode: {RoomCode}, containerId: {ContainerId}", roomCode, containerId);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}
