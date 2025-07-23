using System.Text.Json.Serialization;
using EggLedger.Models.Enums;

namespace EggLedger.Models.Models
{
    public class OrderDetail
    {
        public OrderDetail()
        {
        }

        #region Basic Properties
        public required Guid Id { get; set; } // Foreign Key : Order
        public required Guid OrderId { get; set; } // Foreign Key : Container
        public required Guid ContainerId { get; set; }
        public required int Quantity { get; set; }
        public required decimal Amount { get; set; }
        public required OrderDetailStatus Status { get; set; }
        #endregion

        #region Computed Properties
        /// <summary>
        /// Price per unit calculated from Amount and Quantity
        /// </summary>
        public decimal Price => Amount / Quantity;
        #endregion

        #region Navigation Properties
        /// <summary>
        /// Parent order this detail belongs to
        /// </summary>
        [JsonIgnore]
        public virtual Order Order { get; set; } = null!;

        /// <summary>
        /// Container being ordered
        /// </summary>
        public virtual Container Container { get; set; } = null!;
        #endregion
    }
}
