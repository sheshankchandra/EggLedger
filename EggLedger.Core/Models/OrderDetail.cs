using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using EggLedger.Core.Constants;

namespace EggLedger.Core.Models
{
    public class OrderDetail
    {
        public Guid OrderDetailId { get; set; }
        public Guid OrderId { get; set; }
        public Guid ContainerId { get; set; }
        public int DetailQuantity { get; set; }
        public decimal Price { get; set; }
        public OrderDetailStatus OrderDetailStatus { get; set; }

        [JsonIgnore]
        public virtual Order Order { get; set; } = null!;
        public virtual Container Container { get; set; } = null!;
    }
}
