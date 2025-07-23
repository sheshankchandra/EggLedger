using EggLedger.Models.Enums;

namespace EggLedger.Models.Models
{
    public class Container : AuditableEntity
    {
        public Container()
        {
            Status = ContainerStatus.Available;
        }

        #region Basic Properties
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required DateTime PurchaseDateTime { get; set; } // Foreign Key : User
        public required Guid BuyerId { get; set; }
        public required int TotalQuantity { get; set; }
        public required int RemainingQuantity { get; set; }
        public required decimal Amount { get; set; } // Foreign Key : Room
        public required Guid RoomId { get; set; }
        
        /// <summary>
        /// Status of the container (Available, Depleted, Archived, Suspended)
        /// </summary>
        public ContainerStatus Status { get; set; }
        public DateTime? CompletedDateTime { get; set; }
        #endregion

        #region Computed Properties
        /// <summary>
        /// Price per unit calculated from Amount and TotalQuantity
        /// </summary>
        public decimal Price => Amount / TotalQuantity;

        /// <summary>
        /// Checks if the container is currently available for orders
        /// </summary>
        public bool IsAvailable => Status == ContainerStatus.Available && RemainingQuantity > 0;

        /// <summary>
        /// Checks if the container is archived (soft deleted)
        /// </summary>
        public bool IsArchived => Status == ContainerStatus.Archived;

        /// <summary>
        /// Checks if the container is depleted (no remaining quantity)
        /// </summary>
        public bool IsDepleted => RemainingQuantity <= 0 || Status == ContainerStatus.Depleted;
        #endregion

        #region Navigation Properties
        /// <summary>
        /// User who purchased this container
        /// </summary>
        public User Buyer { get; set; } = null!;

        /// <summary>
        /// Room this container belongs to
        /// </summary>
        public Room Room { get; set; } = null!;

        /// <summary>
        /// Order details associated with this container
        /// </summary>
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>(); 
        #endregion
    }
}
