using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EggLedger.Core.DTOs.Container
{
    public class ContainerCreateDto
    {
        public required string ContainerName { get; set; }
        public required int TotalQuantity { get; set; }
        public required decimal Amount { get; set; }
        public required Guid BuyerId { get; set; }
    }
}
