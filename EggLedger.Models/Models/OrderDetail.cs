using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using EggLedger.Models.Enums;

namespace EggLedger.Models.Models;

public class OrderDetail
{
    public required Guid OrderDetailId { get; set; }
    public required Guid OrderId { get; set; }
    public required Guid ContainerId { get; set; }
    public required int DetailQuantity { get; set; }
    public required OrderDetailStatus OrderDetailStatus { get; set; }

    [NotMapped]
    public decimal Amount => DetailQuantity * Container.Price;

    [JsonIgnore]
    public virtual Order Order { get; set; } = null!;
    public virtual Container Container { get; set; } = null!;
}
