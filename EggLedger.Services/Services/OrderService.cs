using System.Diagnostics;
using EggLedger.Data;
using EggLedger.DTO.Order;
using EggLedger.Models.Enums;
using EggLedger.Models.Models;
using EggLedger.Services.Interfaces;
using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EggLedger.Services.Services
{
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
                User user = await _context.Users.FirstAsync(u => u.Id == userId, cancellationToken);
                UserRoom userRoom = await _context.UserRooms.FirstAsync(u => u.UserId == userId && u.Room.Code == roomCode, cancellationToken);

                Result<string> orderNameResult = await _helperService.GenerateOrderName(user ?? throw new InvalidOperationException(), 1, cancellationToken);
                if (orderNameResult.IsFailed)
                {
                    _logger.LogWarning("Unable to generate the order name for user: {Id}", userId);
                    return Result.Fail("Failed generating an order name");
                }

                _logger.LogInformation("Creating container {Name} with quantity {Quantity} and price {Amount}", dto.ContainerName, dto.Quantity, dto.Amount);

                Order order = new Order
                {
                    Id = Guid.NewGuid(),
                    Name = orderNameResult.Value,
                    Datestamp = DateTime.UtcNow,
                    Type = OrderType.Stocking,
                    Quantity = dto.Quantity,
                    UserId = user.Id,
                    Amount = dto.Amount,
                    Status = OrderStatus.Entered
                };

                Container container = new Container
                {
                    Id = Guid.NewGuid(),
                    Name = string.IsNullOrEmpty(dto.ContainerName) ? $"{user.FirstName} {DateTime.UtcNow:yyyyMMddHHmmss}" : dto.ContainerName,
                    PurchaseDateTime = DateTime.UtcNow,
                    BuyerId = userId,
                    TotalQuantity = dto.Quantity,
                    RemainingQuantity = dto.Quantity,
                    Amount = dto.Amount,
                    RoomId = (Guid)userRoom!.RoomId
                };

                OrderDetail orderDetail = new OrderDetail
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    ContainerId = container.Id,
                    Quantity = dto.Quantity,
                    Amount = dto.Amount,
                    Status = OrderDetailStatus.Entered
                };

                order.OrderDetails.Add(orderDetail);
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Stock order created: {Id}", order.Id);

                return Result.Ok(order.Name);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "CreateStockOrderAsync was canceled for userId {Id}, roomCode {Code}", userId, roomCode);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in CreateStockOrderAsync for userId {Id}, roomCode {Code}", userId, roomCode);
                return Result.Fail("An error occurred while creating the stock order.");
            }
        }

        public async Task<Result<string>> CreateConsumeOrderAsync(Guid userId, int roomCode, ConsumeOrderDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                User user = await _context.Users.FirstAsync(u => u.Id == userId, cancellationToken);
                UserRoom userRoom = await _context.UserRooms.FirstAsync(ur => ur.UserId == userId && ur.Room.Code == roomCode, cancellationToken);

                Result<string> orderNameResult = await _helperService.GenerateOrderName(user!, 2, cancellationToken);
                if (orderNameResult.IsFailed)
                {
                    _logger.LogWarning("Unable to generate the order name for user: {Id}", userId);
                    return Result.Fail("Failed generating an order name");
                }

                Order order = new Order
                {
                    Id = Guid.NewGuid(),
                    Name = orderNameResult.Value,
                    Datestamp = DateTime.UtcNow,
                    Type = OrderType.Consuming,
                    Quantity = dto.Quantity,
                    UserId = userId,
                    Amount = 0, //Update later based on consumed containers
                    Status = OrderStatus.Entered
                };

                int remainingPick = dto.Quantity;

                // Only select containers with available stock, ordered by purchase date
                List<Container> availableContainers = await _context.Containers
                    .Where(c => c.RemainingQuantity > 0)
                    .Where(c => c.RoomId == userRoom.RoomId)
                    .OrderBy(c => c.PurchaseDateTime)
                    .ToListAsync(cancellationToken);

                foreach (Container container in availableContainers)
                {
                    if (remainingPick <= 0)
                        break;

                    int taken = Math.Min(remainingPick, container.RemainingQuantity);

                    order.OrderDetails.Add(new OrderDetail
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        ContainerId = container.Id,
                        Quantity = taken,
                        Amount = container.Price * taken,
                        Status = OrderDetailStatus.Entered
                    });

                    container.RemainingQuantity -= taken;
                    remainingPick -= taken;
                }

                if (remainingPick > 0)
                {
                    _logger.LogWarning("Not enough eggs in stock to fulfill this consumption. Needed: {Needed}, Remaining: {Remaining}", dto.Quantity, remainingPick);
                    return Result.Fail($"Not enough eggs in stock to fulfill this consumption. Needed: {dto.Quantity}, Remaining: {remainingPick}");
                }

                if (remainingPick < 0)
                {
                    _logger.LogError("More than required eggs are consumed. Needed : {Needed}, Consumed: {Consumed}", dto.Quantity, dto.Quantity - remainingPick);
                    return Result.Fail($"More than required eggs are consumed. Needed: {dto.Quantity}, Consumed: {dto.Quantity - remainingPick}");
                }

                order.Amount = order.OrderDetails.Sum(d => d.Amount);

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Consume order created: {Id}", order.Id);

                return Result.Ok(order.Name);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "CreateConsumeOrderAsync was canceled for userId {Id}, roomCode {Code}", userId, roomCode);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in CreateConsumeOrderAsync for userId {Id}, roomCode {Code}", userId, roomCode);
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
                    .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

                if (order == null)
                    return Result.Fail("Order not found");

                var dto = MapToOrderDto(order);
                return Result.Ok(dto);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "GetOrderByIdAsync was canceled for orderId {Id}", orderId);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetOrderByIdAsync for orderId {Id}", orderId);
                return Result.Fail("An error occurred while retrieving the order.");
            }
        }

        public async Task<Result<List<OrderDto>>> GetOrdersByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

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
                _logger.LogInformation(ex, "GetOrdersByUserAsync was canceled for userId {Id}", userId);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetOrdersByUserAsync for userId {Id}", userId);
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
                _logger.LogInformation(ex, "GetOrdersByContainerAsync was canceled for containerId {Id}", containerId);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetOrdersByContainerAsync for containerId {Id}", containerId);
                return Result.Fail("An error occurred while retrieving orders for the container.");
            }
        }

        // Helper method to map Order to OrderDto
        public OrderDto MapToOrderDto(Order order)
        {
            return new OrderDto
            {
                OrderId = order.Id,
                OrderName = order.Name,
                Datestamp = order.Datestamp,
                OrderType = order.Type,
                Quantity = order.Quantity,
                Amount = order.Amount,
                UserId = order.UserId,
                OrderStatus = order.Status,
                OrderDetails = order.OrderDetails.Select(od => new OrderDetailDto
                {
                    OrderDetailId = od.Id,
                    ContainerId = od.ContainerId,
                    DetailQuantity = od.Quantity,
                    Price = od.Price,
                    OrderDetailStatus = od.Status
                }).ToList()
            };
        }
    }
}
