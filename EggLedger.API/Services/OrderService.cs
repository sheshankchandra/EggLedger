using EggLedger.API.Data;
using EggLedger.Core.Constants;
using EggLedger.Core.DTOs.Order;
using EggLedger.Core.Interfaces;
using EggLedger.Core.Models;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace EggLedger.API.Services
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

        public async Task<Result<string>> CreateStockOrderAsync(Guid userId, int roomCode, StockOrderDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            var userRoom = await _context.UserRooms
                .FirstOrDefaultAsync(u => u.UserId == userId && u.Room.RoomCode == roomCode);

            var orderNameResult = await _helperService.GenerateOrderName(user, 1);
            if (orderNameResult.IsFailed)
            {
                _logger.LogWarning("Unable to generate the order name for user: {UserId}", userId);
                return Result.Fail("Failed generating an order name");
            }

            var order = new Order
            {
                OrderId = Guid.NewGuid(),
                OrderName = orderNameResult.Value,
                Datestamp = _helperService.GetIndianTime(),
                OrderType = OrderType.Stocking,
                Quantity = dto.Quantity,
                UserId = user.UserId,
                Amount = dto.Amount,
                OrderStatus = OrderStatus.Entered
            };

            var orderDetail = new OrderDetail
            {
                OrderDetailId = Guid.NewGuid(),
                OrderId = order.OrderId,
                DetailQuantity = 0,
                OrderDetailStatus = OrderDetailStatus.Entered,
                Container = new Container
                {
                    ContainerId = Guid.NewGuid(),
                    ContainerName = string.IsNullOrEmpty(dto.ContainerName) ? $"{user.FirstName} {_helperService.GetIndianTime():yyyyMMddHHmmss}" : dto.ContainerName,
                    PurchaseDateTime = _helperService.GetIndianTime(),
                    BuyerId = userId,
                    TotalQuantity = dto.Quantity,
                    RemainingQuantity = dto.Quantity,
                    Amount = dto.Amount,
                    RoomId = (Guid)userRoom.RoomId
                }
            };

            order.OrderDetails.Add(orderDetail);

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Stock order created: {OrderId}", order.OrderId);

            return Result.Ok(order.OrderName);
        }

        public async Task<Result<string>> CreateConsumeOrderAsync(Guid userId, int roomCode, ConsumeOrderDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);
            var userRoom = await _context.UserRooms
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.Room.RoomCode == roomCode);

            var orderNameResult = await _helperService.GenerateOrderName(user, 2);
            if (orderNameResult.IsFailed)
            {
                _logger.LogWarning("Unable to generate the order name for user: {UserId}", userId);
                return Result.Fail("Failed generating an order name");
            }

            var order = new Order
            {
                OrderId = Guid.NewGuid(),
                OrderName = orderNameResult.Value,
                Datestamp = _helperService.GetIndianTime(),
                OrderType = OrderType.Consuming,
                Quantity = dto.Quantity,
                UserId = userId,
                Amount = 0,
                OrderStatus = OrderStatus.Entered
            };

            int remainingPick = dto.Quantity;

            // Only select containers with available stock, ordered by purchase date
            var availableContainers = await _context.Containers
                .Where(c => c.RemainingQuantity > 0)
                .Where(c => c.RoomId == userRoom.RoomId)
                .OrderBy(c => c.PurchaseDateTime)
                .ToListAsync();

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
                    Price = container.Price,
                    Amount = container.Price * taken,
                    OrderDetailStatus = OrderDetailStatus.Entered
                });

                container.RemainingQuantity -= taken;
                remainingPick -= taken;
            }

            if (remainingPick > 0)
            {
                _logger.LogWarning("Not enough eggs in stock to fulfill this consumption. Needed: {Needed}, Remaining: {Remaining}", dto.Quantity, remainingPick);
            }
            
            if (remainingPick < 0)
            {
                _logger.LogError("More than required eggs are consumed. Needed : {Needed}, Consumed: {Consumed}", dto.Quantity, dto.Quantity - remainingPick);
                return Result.Fail("More than required eggs are consumed.");
            }

            order.Amount = order.OrderDetails.Sum(d => d.Price * d.DetailQuantity);

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Consume order created: {OrderId}", order.OrderId);

            return Result.Ok(order.OrderName);
        }

        public async Task<Result<OrderDto>> GetOrderByIdAsync(Guid orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Container)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return Result.Fail("Order not found");

            var dto = MapToOrderDto(order);
            return Result.Ok(dto);
        }

        public async Task<Result<List<OrderDto>>> GetOrdersByUserAsync(Guid userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return Result.Fail("User not found");
            }

            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Container)
                .ToListAsync();

            var dtos = new List<OrderDto>();
            dtos = orders.Select(MapToOrderDto).ToList();
            return Result.Ok(dtos);
        }

        public async Task<Result<List<OrderDto>>> GetOrdersByContainerAsync(Guid containerId)
        {
            var orders = await _context.Orders
                .Where(o => o.OrderDetails.Any(od => od.ContainerId == containerId))
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Container)
                .ToListAsync();

            var dtos = orders.Select(MapToOrderDto).ToList();
            return Result.Ok(dtos);
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
                    Price = od.Price,
                    OrderDetailStatus = od.OrderDetailStatus
                }).ToList()
            };
        }
    }
}
