using EggLedger.Core.Models;

namespace EggLedger.Core.DTOs
{
    public class EggTransactionDto
    {
        public Guid UserId { get; set; }
        public int Quantity { get; set; }
        public DateTime Date { get; set; }
        public TransactionType Type { get; set; }
        public decimal Amount { get; set; }
    }
}
