using EggLedger.Core.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EggLedger.Core.DTOs
{
    public class ConsumingOrderDto
    {
        public Guid UserId { get; set; }
        public int Quantity { get; set; }
        public DateTime Date { get; set; }
        public OrderType Type { get; set; }
    }
}
