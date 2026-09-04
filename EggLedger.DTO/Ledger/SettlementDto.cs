namespace EggLedger.DTO.Ledger;

public class SettlementDto
{
    public required Guid SettlementId { get; set; }
    public required Guid PayerId { get; set; }
    public required string PayerName { get; set; }
    public required Guid ReceiverId { get; set; }
    public required string ReceiverName { get; set; }
    public required decimal Amount { get; set; }
    public required DateTime Datestamp { get; set; }
    public string? Note { get; set; }
}
