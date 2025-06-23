using EggLedger.API.Data;
using EggLedger.Core.Interfaces;
using EggLedger.Core.Models;
using FluentResults;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace EggLedger.Core.Helpers
{
    public class NamingService : INamingService
    {
        private readonly ApplicationDbContext _context;

        public NamingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Result<string>> GenerateOrderName(User user, int i)
        {
            int serialNumber = 1;
            string userName = user.FirstName;
            string orderPrefix = (i == 1 ? "SO" : "CO");

            int userOrdersCount = await _context.Orders
                .Where(o => o.UserId == user.UserId)
                .CountAsync();

            if (userOrdersCount != 0)
            {
                serialNumber += userOrdersCount;
            }

            string orderName = $"{orderPrefix}-{userName}-{serialNumber}";

            return Result.Ok(orderName);
        }

        public async Task<Result<string>> GenerateContainerName(User user)
        {
            int serialNumber = 1;
            string userName = user.FirstName;
            string containerPrefix = "CNT";

            int userContainersCount = await _context.Containers
                .Where(o => o.BuyerId == user.UserId)
                .CountAsync();

            if (userContainersCount != 0)
            {
                serialNumber += userContainersCount;
            }

            string containerName = $"{containerPrefix}-{userName}-{serialNumber}";

            return Result.Ok(containerName);
        }
    }
}
