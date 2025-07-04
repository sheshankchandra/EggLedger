namespace EggLedger.DTO.Order
{
    public class StockOrderDto
    {
        public string? ContainerName { get; set; }
        public int Quantity { get; set; }
        public decimal Amount { get; set; }
    }
}
