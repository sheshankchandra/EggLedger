using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EggLedger.Core.Models
{
    public class Transaction
    {
        public Guid TransactionId { get; set; }
        public Guid OrderId { get; set; }
        public DateTime Datestamp { get; set; }
        public Guid PayerId { get; set; }
        public Guid ReceiverId { get; set; }
        public int Amount { get; set; }
        public int Status { get; set; }
    }
}
