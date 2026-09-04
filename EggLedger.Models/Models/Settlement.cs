namespace EggLedger.Models.Models;

/// <summary>
/// A recorded settle-up between two room members: Payer paid Receiver Amount in real life
/// (cash, GPay, etc.) to clear some of the debt the ledger computed from consumption history.
/// Only the Receiver records a settlement (they are the one confirming money was received),
/// which keeps a Payer from unilaterally erasing a debt they never actually paid.
/// </summary>
public class Settlement
{
    public Guid SettlementId { get; set; }
    public required Guid RoomId { get; set; }
    public required Guid PayerId { get; set; }
    public required Guid ReceiverId { get; set; }
    public required decimal Amount { get; set; }
    public required DateTime Datestamp { get; set; }
    public string? Note { get; set; }

    // Navigation properties
    public virtual Room Room { get; set; } = null!;
    public virtual User Payer { get; set; } = null!;
    public virtual User Receiver { get; set; } = null!;
}
