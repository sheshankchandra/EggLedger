namespace EggLedger.DTO.Ledger;

/// <summary>
/// A directed amount of money owed: FromUser owes ToUser Amount. Used both for the detailed
/// pairwise breakdown (every nonzero pair in the room) and the simplified settle-up plan
/// (the minimal set of payments that clears every balance).
/// </summary>
public class LedgerEntryDto
{
    public required Guid FromUserId { get; set; }
    public required string FromUserName { get; set; }
    public required Guid ToUserId { get; set; }
    public required string ToUserName { get; set; }
    public required decimal Amount { get; set; }
}
