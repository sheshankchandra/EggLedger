using System.Diagnostics;
using EggLedger.API.Data;
using EggLedger.Core.Constants;
using EggLedger.Core.DTOs;
using EggLedger.Core.Interfaces;
using EggLedger.Core.Models;
using Microsoft.AspNetCore.Mvc;
using ReturnTypeAndStatusCodes.Models;
using Microsoft.EntityFrameworkCore;

namespace EggLedger.API.Services
{
    public class OrderImportService(ApplicationDbContext context, INamingService namingService) : IOrderImportService
    {
        private IOrderImportService _orderImportServiceImplementation;

        public async Task<ActionResult<Order>> StockOrderAsync(StockingOrderDto dto)
        {
            var user = context.Users
                .FirstOrDefault(u => u.UserId == dto.UserId);

            if (user == null)
            {
                return StatusCode(500, "User not found");
            }

            ActionResult<string> orderName = await namingService.GenerateOrderName(user);

            Order order = new Order
            {
                OrderId = Guid.NewGuid(),
                OrderName = orderName.Value,
                Datestamp = dto.Date,
                OrderType = OrderType.Stocking,
                Quantity = dto.Quantity,
                UserId = dto.UserId,
                Amount = dto.Amount,
                OrderStatus = OrderStatus.Entered
            };

            OrderDetail orderDetail = new OrderDetail
            {
                OrderDetailId = Guid.NewGuid(),
                OrderId = order.OrderId,
                DetailQuantity = 0,
                OrderDetailStatus = OrderDetailStatus.Entered,
                Container = new Container()
                {
                    ContainerId = Guid.NewGuid(),
                    ContainerName = user.FirstName + DateTime.UtcNow,
                    PurchaseDateTime = dto.Date,
                    BuyerId = dto.UserId,
                    TotalQuantity = dto.Quantity,
                    RemainingQuantity = dto.Quantity,
                    Amount = dto.Amount
                }
            };

            order.OrderDetails.Add(orderDetail);

            context.Orders.Add(order);
            await context.SaveChangesAsync();
            return order;
        }

        public async Task<ActionResult<Order>> ConsumeOrderAsync(ConsumingOrderDto dto)
        {
            var user = context.Users
                .FirstOrDefault(u => u.UserId == dto.UserId);

            if (user == null)
            {
                return Results.BadRequest("User not found");
            }

            ActionResult<string> orderName = await namingService.GenerateOrderName(user);

            var order = new Order
            {
                OrderId = Guid.NewGuid(),
                OrderName = orderName.Value,
                Datestamp = dto.Date,
                OrderType = OrderType.Consuming,
                Quantity = dto.Quantity,
                UserId = dto.UserId,
                //order.Amount = Gets updated when we call UpdateAmount() method
                OrderStatus = EggLedger.Core.Constants.OrderStatus.Entered
            };

            var remainingPick = dto.Quantity;

            var availableContainers = await context.Containers
                .Where(c => c.RemainingQuantity > 0)
                .OrderBy(c => c.PurchaseDateTime)
                .ToListAsync();

            foreach (var container in availableContainers)
            {
                if (remainingPick <= 0)
                {
                    break;
                }
                if (container.RemainingQuantity <= 0)
                {
                    continue;
                }

                int taken = Math.Min(remainingPick, container.RemainingQuantity);

                order.OrderDetails.Add(new OrderDetail
                {
                    OrderDetailId = Guid.NewGuid(),
                    OrderId = order.OrderId,
                    ContainerId = container.ContainerId,
                    DetailQuantity = taken,
                    Price = container.Price,
                    OrderDetailStatus = OrderDetailStatus.Entered
                });

                container.RemainingQuantity -= taken;
                remainingPick -= taken;
            }

            if (remainingPick > 0)
            {
                throw new InvalidOperationException("Not enough eggs in stock to fulfill this consumption.");
                return null;
            }

            order.UpdateAmount();
            context.Orders.Add(order);
            await context.SaveChangesAsync();
            return order;
        }
    }
}
