namespace EggLedger.Core.DTOs.Container
{
    public class ContainerUpdateDto
    {
        public string? ContainerName { get; set; }
        public DateTime? PurchaseDateTime { get; set; }
        public int? TotalQuantity { get; set; }
        public int? RemainingQuantity { get; set; }
        public decimal? Amount { get; set; }
    }
}
