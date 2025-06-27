using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EggLedger.Core.Models
{
    public class UserRoom
    {
        public int Id { get; set; }
        public Guid RoomId { get; set; }
        public Guid UserId { get; set; }
        public bool IsAdmin { get; set; }
        public DateTime JoinedAt { get; set; }

        // Navigation properties
        public virtual Room Room { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
