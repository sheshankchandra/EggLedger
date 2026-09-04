using System.ComponentModel.DataAnnotations;

namespace EggLedger.DTO.Ledger;

/// <summary>
/// Records that the caller (the Receiver, inferred from the JWT) received Amount from PayerId
/// in real life. Only the person who received money can confirm it - the payer cannot record a
/// settlement on their own behalf, which would let them unilaterally erase a debt.
/// </summary>
public class SettlementCreateDto
{
    [Required]
    public Guid PayerId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}
