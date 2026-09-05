using EggLedger.Data;
using EggLedger.DTO.Activity;
using EggLedger.Models.Enums;
using EggLedger.Services.Extensions;
using EggLedger.Services.Interfaces;
using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EggLedger.Services.Services;

public class ActivityService : IActivityService
{
    // A room's activity feed merges three independent sources (orders, settlements, joins) that
    // can't be paginated with a single SQL query. Household-room activity volume is small enough
    // that fetching a bounded window from each source and sorting/paging the merged list in
    // memory is simple and correct - unlike paginating each source separately, which would give
    // wrong results the moment one source dominates a page.
    private const int SourceFetchCap = 200;

    private readonly ApplicationDbContext _context;
    private readonly ILogger<ActivityService> _logger;

    public ActivityService(ApplicationDbContext context, ILogger<ActivityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<List<ActivityEventDto>>> GetRoomActivityAsync(int roomCode, int page = 1, int pageSize = 30, CancellationToken cancellationToken = default)
    {
        return await _logger.ExecuteAsync(async () =>
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomCode == roomCode, cancellationToken);
            if (room == null)
                return Result.Fail("Room not found");

            var orders = await _context.Orders
                .Where(o => o.OrderStatus == OrderStatus.Completed && o.OrderDetails.Any(od => od.Container.RoomId == room.RoomId))
                .OrderByDescending(o => o.Datestamp)
                .Take(SourceFetchCap)
                .Select(o => new
                {
                    o.OrderType,
                    o.Datestamp,
                    ActorName = o.User.Name,
                    o.Quantity,
                    o.Amount,
                    // A stock order always has exactly one container; a consume order can span
                    // several via FIFO, so only Stock events get a single meaningful name here.
                    FirstContainerName = o.OrderDetails.Select(od => od.Container.ContainerName).FirstOrDefault(),
                })
                .ToListAsync(cancellationToken);

            var settlements = await _context.Settlements
                .Where(s => s.RoomId == room.RoomId)
                .OrderByDescending(s => s.Datestamp)
                .Take(SourceFetchCap)
                .Select(s => new
                {
                    s.Datestamp,
                    ActorName = s.Payer.Name,
                    CounterpartyName = s.Receiver.Name,
                    s.Amount,
                })
                .ToListAsync(cancellationToken);

            var joins = await _context.UserRooms
                .Where(ur => ur.RoomId == room.RoomId && ur.Status == UserRoomStatus.Approved)
                .OrderByDescending(ur => ur.JoinedAt)
                .Take(SourceFetchCap)
                .Select(ur => new { ur.JoinedAt, ActorName = ur.User.Name })
                .ToListAsync(cancellationToken);

            var events = new List<ActivityEventDto>(orders.Count + settlements.Count + joins.Count);

            events.AddRange(orders.Select(o => new ActivityEventDto
            {
                EventType = o.OrderType == OrderType.Stocking ? ActivityEventType.Stock : ActivityEventType.Consume,
                Timestamp = o.Datestamp,
                ActorName = o.ActorName,
                ContainerName = o.OrderType == OrderType.Stocking ? o.FirstContainerName : null,
                Quantity = o.Quantity,
                Amount = o.OrderType == OrderType.Stocking ? o.Amount : null,
            }));

            events.AddRange(settlements.Select(s => new ActivityEventDto
            {
                EventType = ActivityEventType.Settlement,
                Timestamp = s.Datestamp,
                ActorName = s.ActorName,
                CounterpartyName = s.CounterpartyName,
                Amount = s.Amount,
            }));

            events.AddRange(joins.Select(j => new ActivityEventDto
            {
                EventType = ActivityEventType.MemberJoined,
                Timestamp = j.JoinedAt,
                ActorName = j.ActorName,
            }));

            var paged = events
                .OrderByDescending(e => e.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Result.Ok(paged);
        }, "An error occurred while retrieving room activity.");
    }
}
