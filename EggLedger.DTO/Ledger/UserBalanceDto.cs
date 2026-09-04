namespace EggLedger.DTO.Ledger;

/// <summary>
/// A room member's overall net position: positive means the room owes them money overall,
/// negative means they owe the room overall.
/// </summary>
public class UserBalanceDto
{
    public required Guid UserId { get; set; }
    public required string UserName { get; set; }
    public required decimal NetBalance { get; set; }
}
