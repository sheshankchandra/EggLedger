using EggLedger.Core.Constants;

namespace EggLedger.Core.DTOs
{
    public class StockingOrderDto
    {
        public Guid UserId { get; set; }
        public int Quantity { get; set; }
        public DateTime Date { get; set; }
        public OrderType Type { get; set; }
        public decimal Amount { get; set; }
    }
}
