using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EggLedger.Models.Models
{
    public class UserPassword
    {
        public UserPassword()
        {
        }

        public required Guid Id { get; set; }
        public required Guid UserId { get; set; }
        public required string PasswordHash { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
    }
}
