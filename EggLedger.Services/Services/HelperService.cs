using EggLedger.Data;
using EggLedger.Models.Models;
using EggLedger.Services.Extensions;
using EggLedger.Services.Interfaces;
using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EggLedger.Services.Services;

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
        return await _logger.ExecuteAsync(async () =>
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
        }, "An error occurred while generating order name.");
    }

    public async Task<Result<string>> GenerateContainerName(User user, CancellationToken cancellationToken = default)
    {
        return await _logger.ExecuteAsync(async () =>
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
        }, "An error occurred while generating container name.");
    }

    public int GenerateNewRoomCode()
    {
        // Random.Shared is a shared, thread-safe instance (.NET 6+) - avoids the
        // time-seeded-collision risk of constructing a new Random() per call/iteration.
        int roomCode;
        do
        {
            roomCode = Random.Shared.Next(100000, 1000000);
        } while (_context.Rooms.Any(c => c.RoomCode == roomCode));

        return roomCode;
    }
}
