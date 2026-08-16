using System.Text.Json.Serialization;
using EggLedger.Models.Enums;

namespace EggLedger.Models.Models;

public class Order
{
    public Order()
    {
    }

    public required Guid OrderId { get; set; }
    public required string OrderName { get; set; }
    public required DateTime Datestamp { get; set; }
    public required OrderType OrderType { get; set; }
    public required int Quantity { get; set; }
    public required Guid UserId { get; set; }
    public required decimal Amount { get; set; }
    public required OrderStatus OrderStatus { get; set; }


    public User User { get; set; } = null!;
    [JsonIgnore]
    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
