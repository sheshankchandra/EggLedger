using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EggLedger.Core.Models
{
    public class Transaction
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User? User { get; set; }
        public int Quantity { get; set; }
        public DateTime Date { get; set; }
        public TransactionType Type { get; set; }
        public decimal? PricePerEgg { get; set; }
        public virtual ICollection<TransactionDetail> TransactionDetail { get; set; } = null!;
    }
}
