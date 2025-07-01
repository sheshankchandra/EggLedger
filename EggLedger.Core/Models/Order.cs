using EggLedger.Core.Constants;
using System.Text.Json.Serialization;

namespace EggLedger.Core.Models
{
    public class Order
    {
        public Order()
        {
        }

        public Guid OrderId { get; set; }
        public required string OrderName { get; set; }
        public DateTime Datestamp { get; set; }
        public OrderType OrderType { get; set; }
        public int Quantity { get; set; }
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public OrderStatus OrderStatus { get; set; }


        public User User { get; set; } = null!;
        [JsonIgnore]
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
