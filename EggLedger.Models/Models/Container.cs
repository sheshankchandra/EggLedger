using EggLedger.Models.Enums;

namespace EggLedger.Models.Models
{
    public class Container : AuditableEntity
    {
        public Container()
        {
        }

        public Guid ContainerId { get; set; }
        public required string ContainerName { get; set; }
        public required DateTime PurchaseDateTime { get; set; }
        public required Guid BuyerId { get; set; }
        public required int TotalQuantity { get; set; }
        public required int RemainingQuantity { get; set; }
        public required decimal Amount { get; set; }
        public required Guid RoomId { get; set; }
        
        /// <summary>
        /// Status of the container (Available, Depleted, Archived, Suspended)
        /// </summary>
        public ContainerStatus Status { get; set; } = ContainerStatus.Available;
        
        public decimal Price => Amount / TotalQuantity;
        public DateTime? CompletedDateTime { get; set; }
        
        // Navigation properties
        public User Buyer { get; set; } = null!;
        public Room Room { get; set; } = null!;
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

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
    }
}
