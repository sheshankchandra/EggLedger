namespace EggLedger.Models.Models
{
    public class Container
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
        public decimal Price => Amount / TotalQuantity;
        public DateTime? CompletedDateTime { get; set; }
        public User Buyer { get; set; } = null!;
        public Room Room { get; set; } = null!;
        
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
