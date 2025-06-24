using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EggLedger.Core.DTOs.Container
{
    public class ContainerCreateDto
    {
        public string ContainerName { get; set; }
        public DateTime PurchaseDateTime { get; set; }
        public int TotalQuantity { get; set; }
        public decimal Amount { get; set; }
        public Guid BuyerId { get; set; }
    }
}
