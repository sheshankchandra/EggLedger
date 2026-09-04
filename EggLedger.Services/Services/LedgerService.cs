using EggLedger.Data;
using EggLedger.DTO.Ledger;
using EggLedger.Models.Enums;
using EggLedger.Models.Models;
using EggLedger.Services.Extensions;
using EggLedger.Services.Interfaces;
using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EggLedger.Services.Services;

public class LedgerService : ILedgerService
{
    // Amounts this small are treated as "settled" so repeated decimal division/subtraction
    // (e.g. an amount that doesn't divide evenly across containers) never leaves a phantom debt.
    private const decimal Epsilon = 0.01m;

    private readonly ApplicationDbContext _context;
    private readonly ILogger<LedgerService> _logger;

    public LedgerService(ApplicationDbContext context, ILogger<LedgerService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<RoomLedgerDto>> GetRoomLedgerAsync(int roomCode, CancellationToken cancellationToken = default)
    {
        return await _logger.ExecuteAsync(async () =>
        {
            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomCode == roomCode, cancellationToken);
            if (room == null)
                return Result.Fail("Room not found");

            var members = await _context.UserRooms
                .Where(ur => ur.RoomId == room.RoomId)
                .Include(ur => ur.User)
                .ToListAsync(cancellationToken);

            var nameLookup = members.ToDictionary(m => m.UserId, m => m.User.Name);
            var memberIds = nameLookup.Keys.ToHashSet();

            // Gross consumption debt: the consumer owes the container's buyer. Computed from
            // mapped scalar columns only - Container.Price is a computed property EF cannot
            // translate to SQL, so the division is written out against Amount/TotalQuantity.
            var consumptionRows = await _context.OrderDetails
                .Where(od => od.OrderDetailStatus == OrderDetailStatus.Completed
                          && od.Order.OrderStatus == OrderStatus.Completed
                          && od.Order.OrderType == OrderType.Consuming
                          && od.Container.RoomId == room.RoomId
                          && od.Order.UserId != od.Container.BuyerId)
                .Select(od => new
                {
                    DebtorId = od.Order.UserId,
                    CreditorId = od.Container.BuyerId,
                    Amount = od.DetailQuantity * od.Container.Amount / od.Container.TotalQuantity,
                })
                .ToListAsync(cancellationToken);

            var settlementRows = await _context.Settlements
                .Where(s => s.RoomId == room.RoomId)
                .Select(s => new { DebtorId = s.PayerId, CreditorId = s.ReceiverId, s.Amount })
                .ToListAsync(cancellationToken);

            // net[(debtor, creditor)] accumulates every directed amount: consumption adds to
            // what the debtor owes, a settlement subtracts from it (paying reduces the debt).
            var net = new Dictionary<(Guid Debtor, Guid Creditor), decimal>();
            foreach (var row in consumptionRows)
            {
                var key = (row.DebtorId, row.CreditorId);
                net[key] = net.GetValueOrDefault(key) + row.Amount;
            }
            foreach (var row in settlementRows)
            {
                var key = (row.DebtorId, row.CreditorId);
                net[key] = net.GetValueOrDefault(key) - row.Amount;
            }

            // Any user involved in a debt who has since left the room still needs a name and a
            // balance entry, even though they are no longer in `members`.
            var extraIds = net.Keys.SelectMany(k => new[] { k.Debtor, k.Creditor })
                .Where(id => !memberIds.Contains(id))
                .Distinct()
                .ToList();
            if (extraIds.Count > 0)
            {
                var extraUsers = await _context.Users
                    .Where(u => extraIds.Contains(u.UserId))
                    .ToListAsync(cancellationToken);
                foreach (var user in extraUsers)
                    nameLookup[user.UserId] = user.Name;
            }

            var balances = nameLookup.Keys.ToDictionary(id => id, _ => 0m);

            // Collapse opposing directions between every pair into one signed net amount, so
            // "A owes B 10, B owes A 3" becomes a single "A owes B 7" entry.
            var pairwiseDebts = new List<LedgerEntryDto>();
            var seenPairs = new HashSet<(Guid, Guid)>();

            foreach (var (debtor, creditor) in net.Keys)
            {
                var pairKey = debtor.CompareTo(creditor) < 0 ? (First: debtor, Second: creditor) : (First: creditor, Second: debtor);
                if (!seenPairs.Add(pairKey))
                    continue;

                var forward = net.GetValueOrDefault((pairKey.First, pairKey.Second));
                var backward = net.GetValueOrDefault((pairKey.Second, pairKey.First));
                var diff = Math.Round(forward - backward, 2); // positive => First owes Second

                if (diff > Epsilon)
                {
                    pairwiseDebts.Add(ToEntry(pairKey.First, pairKey.Second, diff, nameLookup));
                    balances[pairKey.First] -= diff;
                    balances[pairKey.Second] += diff;
                }
                else if (diff < -Epsilon)
                {
                    var amount = -diff;
                    pairwiseDebts.Add(ToEntry(pairKey.Second, pairKey.First, amount, nameLookup));
                    balances[pairKey.Second] -= amount;
                    balances[pairKey.First] += amount;
                }
            }

            var balanceDtos = balances
                .Where(kv => memberIds.Contains(kv.Key) || Math.Abs(kv.Value) > Epsilon)
                .Select(kv => new UserBalanceDto
                {
                    UserId = kv.Key,
                    UserName = nameLookup[kv.Key],
                    NetBalance = Math.Round(kv.Value, 2),
                })
                .OrderByDescending(b => b.NetBalance)
                .ToList();

            var suggestedSettlements = SimplifyDebts(balances, nameLookup);

            return Result.Ok(new RoomLedgerDto
            {
                Balances = balanceDtos,
                PairwiseDebts = pairwiseDebts.OrderByDescending(d => d.Amount).ToList(),
                SuggestedSettlements = suggestedSettlements,
            });
        }, "An error occurred while computing the room ledger.");
    }

    public async Task<Result<SettlementDto>> RecordSettlementAsync(Guid receiverId, int roomCode, SettlementCreateDto dto, CancellationToken cancellationToken = default)
    {
        return await _logger.ExecuteAsync(async () =>
        {
            if (dto.PayerId == receiverId)
            {
                return Result.Fail("You cannot record a settlement with yourself.");
            }

            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomCode == roomCode, cancellationToken);
            if (room == null)
                return Result.Fail("Room not found");

            var payerIsMember = await _context.UserRooms
                .AnyAsync(ur => ur.RoomId == room.RoomId && ur.UserId == dto.PayerId, cancellationToken);
            if (!payerIsMember)
            {
                _logger.LogWarning("Rejected settlement: {PayerId} is not a member of room {RoomCode}", dto.PayerId, roomCode);
                return Result.Fail("The payer must be a member of this room.");
            }

            var receiver = await _context.Users.FirstOrDefaultAsync(u => u.UserId == receiverId, cancellationToken);
            var payer = await _context.Users.FirstOrDefaultAsync(u => u.UserId == dto.PayerId, cancellationToken);
            if (receiver == null || payer == null)
                return Result.Fail("User not found");

            var settlement = new Settlement
            {
                SettlementId = Guid.NewGuid(),
                RoomId = room.RoomId,
                PayerId = dto.PayerId,
                ReceiverId = receiverId,
                Amount = dto.Amount,
                Datestamp = DateTime.UtcNow,
                Note = dto.Note,
            };

            _context.Settlements.Add(settlement);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Settlement recorded in room {RoomCode}: {PayerId} paid {ReceiverId} {Amount}",
                roomCode, dto.PayerId, receiverId, dto.Amount);

            return Result.Ok(new SettlementDto
            {
                SettlementId = settlement.SettlementId,
                PayerId = payer.UserId,
                PayerName = payer.Name,
                ReceiverId = receiver.UserId,
                ReceiverName = receiver.Name,
                Amount = settlement.Amount,
                Datestamp = settlement.Datestamp,
                Note = settlement.Note,
            });
        }, "An error occurred while recording the settlement.");
    }

    public async Task<Result<List<SettlementDto>>> GetSettlementHistoryAsync(int roomCode, CancellationToken cancellationToken = default)
    {
        return await _logger.ExecuteAsync(async () =>
        {
            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomCode == roomCode, cancellationToken);
            if (room == null)
                return Result.Fail("Room not found");

            // Materialize first, then project - User.Name is a computed C# property (FirstName +
            // LastName) that EF Core cannot translate into SQL.
            var settlements = await _context.Settlements
                .Where(s => s.RoomId == room.RoomId)
                .Include(s => s.Payer)
                .Include(s => s.Receiver)
                .OrderByDescending(s => s.Datestamp)
                .ToListAsync(cancellationToken);

            var dtos = settlements.Select(s => new SettlementDto
            {
                SettlementId = s.SettlementId,
                PayerId = s.PayerId,
                PayerName = s.Payer.Name,
                ReceiverId = s.ReceiverId,
                ReceiverName = s.Receiver.Name,
                Amount = s.Amount,
                Datestamp = s.Datestamp,
                Note = s.Note,
            }).ToList();

            return Result.Ok(dtos);
        }, "An error occurred while retrieving settlement history.");
    }

    public async Task<Result> DeleteSettlementAsync(int roomCode, Guid settlementId, Guid callerId, CancellationToken cancellationToken = default)
    {
        return await _logger.ExecuteAsync(async () =>
        {
            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomCode == roomCode, cancellationToken);
            if (room == null)
                return Result.Fail("Room not found");

            var settlement = await _context.Settlements
                .FirstOrDefaultAsync(s => s.SettlementId == settlementId && s.RoomId == room.RoomId, cancellationToken);
            if (settlement == null)
                return Result.Fail("Settlement not found");

            if (settlement.ReceiverId != callerId)
            {
                _logger.LogWarning("Rejected settlement delete: {CallerId} is not the receiver of settlement {SettlementId}", callerId, settlementId);
                return Result.Fail("Only the person who recorded this settlement can remove it.");
            }

            _context.Settlements.Remove(settlement);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Settlement {SettlementId} deleted by {CallerId}", settlementId, callerId);
            return Result.Ok();
        }, "An error occurred while deleting the settlement.");
    }

    private static LedgerEntryDto ToEntry(Guid fromUserId, Guid toUserId, decimal amount, Dictionary<Guid, string> nameLookup) => new()
    {
        FromUserId = fromUserId,
        FromUserName = nameLookup[fromUserId],
        ToUserId = toUserId,
        ToUserName = nameLookup[toUserId],
        Amount = amount,
    };

    /// <summary>
    /// Splitwise-style debt simplification: greedily match the largest remaining debtor against
    /// the largest remaining creditor until every balance clears. This never produces more than
    /// (participants with a nonzero balance - 1) suggested payments, regardless of how tangled
    /// the underlying pairwise debts are.
    /// </summary>
    private static List<LedgerEntryDto> SimplifyDebts(Dictionary<Guid, decimal> balances, Dictionary<Guid, string> nameLookup)
    {
        var creditors = balances.Where(kv => kv.Value > Epsilon)
            .Select(kv => (Id: kv.Key, Amount: kv.Value))
            .OrderByDescending(x => x.Amount)
            .ToList();
        var debtors = balances.Where(kv => kv.Value < -Epsilon)
            .Select(kv => (Id: kv.Key, Amount: -kv.Value))
            .OrderByDescending(x => x.Amount)
            .ToList();

        var suggestions = new List<LedgerEntryDto>();
        int creditorIndex = 0, debtorIndex = 0;

        while (creditorIndex < creditors.Count && debtorIndex < debtors.Count)
        {
            var creditor = creditors[creditorIndex];
            var debtor = debtors[debtorIndex];
            var settled = Math.Round(Math.Min(creditor.Amount, debtor.Amount), 2);

            if (settled > Epsilon)
            {
                suggestions.Add(ToEntry(debtor.Id, creditor.Id, settled, nameLookup));
            }

            creditors[creditorIndex] = (creditor.Id, creditor.Amount - settled);
            debtors[debtorIndex] = (debtor.Id, debtor.Amount - settled);

            if (creditors[creditorIndex].Amount <= Epsilon) creditorIndex++;
            if (debtors[debtorIndex].Amount <= Epsilon) debtorIndex++;
        }

        return suggestions;
    }
}
