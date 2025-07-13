using EggLedger.Models.Enums;

namespace EggLedger.DTO.Container
{
    public class ContainerSummaryDto
    {
        public required Guid ContainerId { get; set; }
        public required string ContainerName { get; set; }
        public required DateTime PurchaseDateTime { get; set; }
        public required string BuyerName { get; set; }
        public required int TotalQuantity { get; set; }
        public required int RemainingQuantity { get; set; }
        public required decimal Amount { get; set; }
        public required string RoomName { get; set; }
        public required ContainerStatus Status { get; set; }
        public required decimal Price { get; set; }
        public DateTime? CompletedDateTime { get; set; }
    }
}
