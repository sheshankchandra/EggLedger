using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EggLedger.Core.Models
{
    public class Content
    {
        public Guid Id { get; set; }
        public Guid BroughtByUserId { get; set; }
        public User? BroughtByUser { get; set; }
        public int TotalEggs { get; set; }
        public int RemainingEggs { get; set; }
        public decimal Amount { get; set; }
        public DateTime PurchaseDateTime { get; set; }
        public DateTime? CompletedDateTime { get; set; }
        public List<Transaction>? Transactions { get; set; }
    }
}
