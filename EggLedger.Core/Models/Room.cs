using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EggLedger.Core.Models
{
    public class Room
    {
        public required Guid RoomId { get; set; }          // Primary Key
        public required Guid AdminUserId { get; set; }
        public required string RoomName { get; set; }
        public required int Code { get; set; }
        public required bool IsOpen { get; set; }
        public required DateTime CreatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<Container> Containers { get; set; } = new List<Container>();
    }
}
