namespace EggLedger.DTO.Container
{
    public class ContainerSummaryDto
    {
        public Guid ContainerId { get; set; }
        public required string ContainerName { get; set; }
        public DateTime PurchaseDateTime { get; set; }
        public required string BuyerName { get; set; }
        public int TotalQuantity { get; set; }
        public int RemainingQuantity { get; set; }
        public decimal Amount { get; set; }
        public required string RoomName { get; set; }
        public decimal Price { get; set; }
        public DateTime? CompletedDateTime { get; set; }
    }
}
