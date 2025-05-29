using EggLedger.API.Data;
using EggLedger.Core.Constants;
using EggLedger.Core.DTOs;
using EggLedger.Core.Interfaces;
using EggLedger.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EggLedger.API.Services
{
    public class OrderImportService(ApplicationDbContext context) : IOrderImportService
    {
        public async Task<Order> StockOrderAsync(StockingOrderDto dto)
        {
            var order = new Order
            {
                OrderId = Guid.NewGuid(),
                Datestamp = dto.Date,
                OrderType = OrderType.Stocking,
                Quantity = dto.Quantity,
                UserId = dto.UserId,
                Amount = dto.Amount,
                OrderStatus = OrderStatus.Entered
            };

            var orderDetail = new OrderDetail
            {
                OrderDetailId = Guid.NewGuid(),
                OrderId = order.OrderId,
                DetailQuantity = 0,
                OrderDetailStatus = OrderDetailStatus.Entered,
                Container = new Container()
                {
                    ContainerId = Guid.NewGuid(),
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

        public async Task<Order> ConsumeOrderAsync(ConsumingOrderDto dto)
        {
            var order = new Order
            {
                OrderId = Guid.NewGuid(),
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
