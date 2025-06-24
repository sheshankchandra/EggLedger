using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EggLedger.Core.Models
{
    public class User
    {
        public Guid UserId { get; set; }
        public string Name => FirstName.Trim() + (string.IsNullOrEmpty(LastName) ? "" : LastName.Trim());
        public string FirstName { get; set; } = null!;
        public string? LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public int Role { get; set; }
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public virtual ICollection<Container> Containers { get; set; } = new List<Container>();
    }
}
