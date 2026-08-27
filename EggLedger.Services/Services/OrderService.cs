using System.Diagnostics;
using EggLedger.Data;
using EggLedger.DTO.Order;
using EggLedger.Models.Enums;
using EggLedger.Models.Models;
using EggLedger.Services.Interfaces;
using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EggLedger.Services.Services;

public class OrderService : IOrderService
{
    private readonly ILogger<OrderService> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IHelperService _helperService;

    public OrderService(ApplicationDbContext context, IHelperService helperService, ILogger<OrderService> logger)
    {
        _context = context;
        _helperService = helperService;
        _logger = logger;
    }

    public async Task<Result<string>> CreateStockOrderAsync(Guid userId, int roomCode, StockOrderDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
            var userRoom = await _context.UserRooms
                .FirstOrDefaultAsync(u => u.UserId == userId && u.Room.RoomCode == roomCode, cancellationToken);

            if (userRoom == null)
            {
                throw new ArgumentException("User is not a member of the specified room.", nameof(roomCode));
            }

            var orderNameResult = await _helperService.GenerateOrderName(user ?? throw new InvalidOperationException(), 1, cancellationToken);
            if (orderNameResult.IsFailed)
            {
                _logger.LogWarning("Unable to generate the order name for user: {UserId}", userId);
                return Result.Fail("Failed generating an order name");
            }

            _logger.LogInformation("Creating container {ContainerName} with quantity {Quantity} and price {Amount}", dto.ContainerName, dto.Quantity, dto.Amount);

            var order = new Order
            {
                OrderId = Guid.NewGuid(),
                OrderName = orderNameResult.Value,
                Datestamp = DateTime.UtcNow,
                OrderType = OrderType.Stocking,
                Quantity = dto.Quantity,
                UserId = user.UserId,
                Amount = dto.Amount,
                OrderStatus = OrderStatus.Completed
            };

            var container = new Container
            {
                ContainerId = Guid.NewGuid(),
                ContainerName = string.IsNullOrEmpty(dto.ContainerName) ?
                    $"{user.FirstName} {DateTime.UtcNow:yyyyMMddHHmmss}" :
                    dto.ContainerName,
                PurchaseDateTime = DateTime.UtcNow,
                BuyerId = userId,
                TotalQuantity = dto.Quantity,
                RemainingQuantity = dto.Quantity,
                Amount = dto.Amount,
                RoomId = userRoom.RoomId,
            };

            var orderDetail = new OrderDetail
            {
                OrderDetailId = Guid.NewGuid(),
                OrderId = order.OrderId,
                ContainerId = container.ContainerId,
                DetailQuantity = dto.Quantity,
                OrderDetailStatus = OrderDetailStatus.Completed
            };

            order.OrderDetails.Add(orderDetail);

            _context.Containers.Add(container); // Ensure container is added before saving
            _context.Orders.Add(order);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Stock order created: {OrderId}", order.OrderId);

            return Result.Ok(order.OrderName);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation(ex, "CreateStockOrderAsync was canceled for userId {UserId}, roomCode {RoomCode}", userId, roomCode);
            return Result.Fail("Operation was canceled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in CreateStockOrderAsync for userId {UserId}, roomCode {RoomCode}", userId, roomCode);
            return Result.Fail("An error occurred while creating the stock order.");
        }
    }

    public async Task<Result<ConsumeOrderResultDto>> CreateConsumeOrderAsync(Guid userId, int roomCode, ConsumeOrderDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            if (dto.Quantity <= 0)
            {
                return Result.Fail("Quantity must be greater than zero.");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
            var userRoom = await _context.UserRooms
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.Room.RoomCode == roomCode, cancellationToken);

            if (user == null || userRoom == null)
            {
                return Result.Fail("User is not a member of the specified room.");
            }

            var orderNameResult = await _helperService.GenerateOrderName(user, 2, cancellationToken);
            if (orderNameResult.IsFailed)
            {
                _logger.LogWarning("Unable to generate the order name for user: {UserId}", userId);
                return Result.Fail("Failed generating an order name");
            }

            var availableContainers = await _context.Containers
                .Where(c => c.RoomId == userRoom.RoomId && c.RemainingQuantity > 0)
                .OrderBy(c => c.PurchaseDateTime)
                .ToListAsync(cancellationToken);

            var availableQuantity = availableContainers.Sum(c => c.RemainingQuantity);

            var order = new Order
            {
                OrderId = Guid.NewGuid(),
                OrderName = orderNameResult.Value,
                Datestamp = DateTime.UtcNow,
                OrderType = OrderType.Consuming,
                Quantity = dto.Quantity,
                UserId = userId,
                Amount = 0,
                OrderStatus = OrderStatus.Pending
            };

            // A consume that cannot be fully satisfied is recorded as a Failed order so
            // the attempt is auditable, and the caller is told how much is actually available.
            if (availableQuantity < dto.Quantity)
            {
                order.OrderStatus = OrderStatus.Failed;
                _context.Orders.Add(order);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Consume order {OrderId} failed: requested {Requested}, available {Available}",
                    order.OrderId, dto.Quantity, availableQuantity);

                return Result.Ok(new ConsumeOrderResultDto
                {
                    OrderName = order.OrderName,
                    Status = OrderStatus.Failed,
                    RequestedQuantity = dto.Quantity,
                    AvailableQuantity = availableQuantity,
                    Message = $"Not enough stock. Only {availableQuantity} available."
                });
            }

            int remainingPick = dto.Quantity;
            foreach (var container in availableContainers)
            {
                if (remainingPick <= 0)
                    break;

                int taken = Math.Min(remainingPick, container.RemainingQuantity);

                order.OrderDetails.Add(new OrderDetail
                {
                    OrderDetailId = Guid.NewGuid(),
                    OrderId = order.OrderId,
                    ContainerId = container.ContainerId,
                    DetailQuantity = taken,
                    OrderDetailStatus = OrderDetailStatus.Completed,
                    Container = container
                });

                container.RemainingQuantity -= taken;
                remainingPick -= taken;
            }

            order.OrderStatus = OrderStatus.Completed;
            order.Amount = order.OrderDetails.Sum(d => d.Amount);

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Consume order created: {OrderId}", order.OrderId);

            return Result.Ok(new ConsumeOrderResultDto
            {
                OrderName = order.OrderName,
                Status = OrderStatus.Completed,
                RequestedQuantity = dto.Quantity,
                AvailableQuantity = availableQuantity - dto.Quantity,
                Message = null
            });
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation(ex, "CreateConsumeOrderAsync was canceled for userId {UserId}, roomCode {RoomCode}", userId, roomCode);
            return Result.Fail("Operation was canceled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in CreateConsumeOrderAsync for userId {UserId}, roomCode {RoomCode}", userId, roomCode);
            return Result.Fail("An error occurred while creating the consume order.");
        }
    }

    public async Task<Result<OrderDto>> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        try
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Container)
                .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);

            if (order == null)
                return Result.Fail("Order not found");

            var dto = MapToOrderDto(order);
            return Result.Ok(dto);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation(ex, "GetOrderByIdAsync was canceled for orderId {OrderId}", orderId);
            return Result.Fail("Operation was canceled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in GetOrderByIdAsync for orderId {OrderId}", orderId);
            return Result.Fail("An error occurred while retrieving the order.");
        }
    }

    public async Task<Result<List<OrderDto>>> GetOrdersByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

            if (user == null)
            {
                return Result.Fail("User not found");
            }

            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Container)
                .ToListAsync(cancellationToken);

            var dtos = orders.Select(MapToOrderDto).ToList();
            return Result.Ok(dtos);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation(ex, "GetOrdersByUserAsync was canceled for userId {UserId}", userId);
            return Result.Fail("Operation was canceled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in GetOrdersByUserAsync for userId {UserId}", userId);
            return Result.Fail("An error occurred while retrieving orders for the user.");
        }
    }

    public async Task<Result<List<OrderDto>>> GetOrdersByContainerAsync(Guid containerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var orders = await _context.Orders
                .Where(o => o.OrderDetails.Any(od => od.ContainerId == containerId))
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Container)
                .ToListAsync(cancellationToken);

            var dtos = orders.Select(MapToOrderDto).ToList();
            return Result.Ok(dtos);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation(ex, "GetOrdersByContainerAsync was canceled for containerId {ContainerId}", containerId);
            return Result.Fail("Operation was canceled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in GetOrdersByContainerAsync for containerId {ContainerId}", containerId);
            return Result.Fail("An error occurred while retrieving orders for the container.");
        }
    }

    // Helper method to map Order to OrderDto
    public OrderDto MapToOrderDto(Order order)
    {
        return new OrderDto
        {
            OrderId = order.OrderId,
            OrderName = order.OrderName,
            Datestamp = order.Datestamp,
            OrderType = order.OrderType,
            Quantity = order.Quantity,
            Amount = order.Amount,
            UserId = order.UserId,
            OrderStatus = order.OrderStatus,
            OrderDetails = order.OrderDetails.Select(od => new OrderDetailDto
            {
                OrderDetailId = od.OrderDetailId,
                ContainerId = od.ContainerId,
                DetailQuantity = od.DetailQuantity,
                Price = od.Container.Price,
                OrderDetailStatus = od.OrderDetailStatus
            }).ToList()
        };
    }
}
