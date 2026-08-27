using EggLedger.Models.Enums;

namespace EggLedger.DTO.Order;

public class ConsumeOrderResultDto
{
    public required string OrderName { get; set; }
    public OrderStatus Status { get; set; }
    public int RequestedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public string? Message { get; set; }
}
