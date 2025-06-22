using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EggLedger.Core.Models
{
    public class Container()
    {
        public Guid ContainerId { get; set; }
        public required string ContainerName { get; set; }
        public DateTime PurchaseDateTime { get; set; }
        public Guid BuyerId { get; set; }
        public int TotalQuantity { get; set; }
        public int RemainingQuantity { get; set; }
        public decimal Amount { get; set; }
        public decimal Price => Amount / TotalQuantity;
        public DateTime? CompletedDateTime { get; set; }
        public User Buyer { get; set; } = null!;
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
