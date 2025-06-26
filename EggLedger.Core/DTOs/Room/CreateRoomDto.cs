using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EggLedger.Core.DTOs.Room
{
    public class CreateRoomDto
    {
        public Guid AdminUserId { get; set; }
        public string RoomName { get; set; }
        public bool IsOpen { get; set; }
    }
}
