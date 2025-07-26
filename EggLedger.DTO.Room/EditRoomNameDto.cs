namespace EggLedger.DTO.Room
{
    public class EditRoomNameDto
    {
        public required Guid RoomId { get; set; }
        public required string NewRoomName { get; set; }
    }
}
