using EggLedger.Models.Enums;

namespace EggLedger.DTO.Room
{
    public class EditRoomStatusDto
    {
        public required Guid RoomId { get; set; }
        public required RoomStatus NewStatus { get; set; }
    }
}
