using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EggLedger.Core.DTOs.Room
{
    public class UpdateRoomPublicStatusDto
    {
        public required Guid RoomId { get; set; }
        public Guid UserId { get; set; }
        public bool IsOpen { get; set; }
    }
}
