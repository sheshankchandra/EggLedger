namespace EggLedger.DTO.Ledger;

/// <summary>
/// The full "who owes whom" picture for a room: each member's net balance, the detailed
/// pairwise debts (after netting opposing directions and applying recorded settlements), and
/// a simplified minimal-transaction settle-up plan (Splitwise-style debt simplification).
/// </summary>
public class RoomLedgerDto
{
    public required List<UserBalanceDto> Balances { get; set; }
    public required List<LedgerEntryDto> PairwiseDebts { get; set; }
    public required List<LedgerEntryDto> SuggestedSettlements { get; set; }
}
