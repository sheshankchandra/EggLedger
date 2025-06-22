using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EggLedger.Core.DTOs.Container
{
    public class ContainerSummaryDto
    {
        public Guid ContainerId { get; set; }
        public string ContainerName { get; set; }
        public int RemainingQuantity { get; set; }
        public int TotalQuantity { get; set; }
        public string OwnerName { get; set; }
    }
}
