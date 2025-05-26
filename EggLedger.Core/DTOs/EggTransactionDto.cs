namespace EggLedger.Core.DTOs
{
    public class EggTransactionDto
    {
        public int UserId { get; set; }
        public int Quantity { get; set; }
        public DateTime Date { get; set; }
        public string Type { get; set; } = "Consumed";
        public decimal? PricePerEgg { get; set; }
    }
}
