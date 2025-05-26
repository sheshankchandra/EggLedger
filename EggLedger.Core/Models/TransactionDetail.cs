using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EggLedger.Core.Models
{
    public class TransactionDetail
    {
        public Guid Id { get; set; }
        public Guid TransactionId { get; set; }
        public Guid ContentId { get; set; }
        public int EggsTakenFromContent { get; set; }
    }
}
