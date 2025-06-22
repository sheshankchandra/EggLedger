using EggLedger.API.Data;
using EggLedger.Core;
using EggLedger.Core.Interfaces;
using EggLedger.Core.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EggLedger.API.Helpers
{
    public class NamingServiceContext(ApplicationDbContext context) : INamingServiceContext
    {
        public async Task<ActionResult<string>> GenerateOrderName(User user)
        {
            int serialNumber = 1;
            string userName = user.FirstName;

            int userOrdersCount = await context.Orders
                .Where(o => o.UserId == user.UserId)
                .CountAsync();

            if (userOrdersCount != 0)
            {
                serialNumber += userOrdersCount;
            }

            string orderName = $"{userName}-{serialNumber}";

            return orderName;
        }

        public async Task<ActionResult<string>> GenerateContainerName(Guid userId)
        {
            int serialNumber = 1;
            string userName = "NotFound";

            var userOrders = await context.Containers
                .Where(o => o.BuyerId == userId)
                .Select(o => new
                {
                    UserName = o.Buyer.FirstName
                })
                .ToListAsync();

            if (userOrders.Any())
            {
                serialNumber += userOrders.Count();
                userName = userOrders.First().UserName;
            }

            string containerName = $"{userName}-{serialNumber}";

            return containerName;
        }
    }
}
