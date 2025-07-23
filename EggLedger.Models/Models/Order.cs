using System.Text.Json.Serialization;
using EggLedger.Models.Enums;

namespace EggLedger.Models.Models
{
    public class Order
    {
        public Order()
        {
        }

        #region Basic Properties
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required DateTime Datestamp { get; set; }
        public required OrderType Type { get; set; }
        public required int Quantity { get; set; }
        public required Guid UserId { get; set; } // Foreign Key : User
        public required decimal Amount { get; set; }
        public required OrderStatus Status { get; set; }
        #endregion

        #region Computed Properties
        /// <summary>
        /// Price per unit calculated from Amount and Quantity
        /// </summary>
        public decimal Price => Amount / Quantity;
        #endregion

        #region Navigation Properties
        /// <summary>
        /// Associated user who created the order
        /// </summary>
        public User User { get; set; } = null!;
        
        /// <summary>
        /// Collection of order details associated with this order
        /// </summary>
        [JsonIgnore]
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        
        /// <summary>
        /// Collection of transactions associated with this order
        /// </summary>
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        #endregion
    }
}
