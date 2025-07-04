using EggLedger.Models.Enums;

namespace EggLedger.DTO.Order
{
    public class OrderDto
    {
        public Guid OrderId { get; set; }
        public required string OrderName { get; set; }
        public DateTime Datestamp { get; set; }
        public OrderType OrderType { get; set; }
        public int Quantity { get; set; }
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public ICollection<OrderDetailDto> OrderDetails { get; set; } = new List<OrderDetailDto>();
    }
}
