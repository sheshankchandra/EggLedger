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
        public required string ContainerName { get; set; }
        public DateTime PurchaseDateTime { get; set; }
        public string BuyerName { get; set; }
        public int TotalQuantity { get; set; }
        public int RemainingQuantity { get; set; }
        public decimal Amount { get; set; }
        public required string RoomName { get; set; }
        public decimal Price { get; set; }
        public DateTime? CompletedDateTime { get; set; }
    }
}
