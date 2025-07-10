namespace EggLedger.Models.Models
{
    public class Transaction
    {
        public Guid TransactionId { get; set; }
        public Guid OrderId { get; set; }
        public DateTime Datestamp { get; set; }
        public Guid PayerId { get; set; }
        public Guid ReceiverId { get; set; }
        public int Amount { get; set; }
        public int Status { get; set; }

        // Navigation properties
        public virtual User Payer { get; set; } = null!;
        public virtual User Receiver { get; set; } = null!;
        public virtual Order Order { get; set; } = null!;
    }
}
