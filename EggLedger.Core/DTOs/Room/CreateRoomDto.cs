namespace EggLedger.Core.DTOs.Room
{
    public class CreateRoomDto
    {
        public required string RoomName { get; set; }
        public bool IsOpen { get; set; }
    }
}
