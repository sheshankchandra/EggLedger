using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EggLedger.Data;
using EggLedger.Models.Models;
using EggLedger.Services.Interfaces;
using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EggLedger.API.Helpers
{
    public class HelperService : IHelperService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HelperService> _logger;

        public HelperService(ApplicationDbContext context, ILogger<HelperService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Result<string>> GenerateOrderName(User user, int i, CancellationToken cancellationToken = default)
        {
            try
            {
                int serialNumber = 1;
                string userName = user.FirstName;
                string orderPrefix = (i == 1 ? "SO" : "CO");

                int userOrdersCount = await _context.Orders
                    .Where(o => o.UserId == user.UserId)
                    .CountAsync(cancellationToken);

                if (userOrdersCount != 0)
                {
                    serialNumber += userOrdersCount;
                }

                string orderName = $"{orderPrefix}-{userName}-{serialNumber}";

                return Result.Ok(orderName);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "GenerateOrderName was canceled for userId {UserId}", user.UserId);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GenerateOrderName for userId {UserId}", user.UserId);
                return Result.Fail("An error occurred while generating order name.");
            }
        }

        public async Task<Result<string>> GenerateContainerName(User user, CancellationToken cancellationToken = default)
        {
            try
            {
                int serialNumber = 1;
                string userName = user.FirstName;
                string containerPrefix = "CNT";

                int userContainersCount = await _context.Containers
                    .Where(o => o.BuyerId == user.UserId)
                    .CountAsync(cancellationToken);

                if (userContainersCount != 0)
                {
                    serialNumber += userContainersCount;
                }

                string containerName = $"{containerPrefix}-{userName}-{serialNumber}";

                return Result.Ok(containerName);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "GenerateContainerName was canceled for userId {UserId}", user.UserId);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GenerateContainerName for userId {UserId}", user.UserId);
                return Result.Fail("An error occurred while generating container name.");
            }
        }

        public int GenerateNewRoomCode()
        {
            int roomCode = new Random().Next(100000, 1000000);

            while (_context.Rooms.Any(c => c.RoomCode == roomCode))
            {
                roomCode = new Random().Next(100000, 1000000);
            }

            return roomCode;
        }
    }
}
