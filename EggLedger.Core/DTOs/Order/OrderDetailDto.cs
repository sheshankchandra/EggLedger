using EggLedger.Core.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EggLedger.Core.DTOs.Order
{
    public class OrderDetailDto
    {
        public Guid OrderDetailId { get; set; }
        public Guid ContainerId { get; set; }
        public int DetailQuantity { get; set; }
        public decimal Price { get; set; }
        public OrderDetailStatus OrderDetailStatus { get; set; }
    }
}
