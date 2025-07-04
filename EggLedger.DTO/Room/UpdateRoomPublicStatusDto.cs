namespace EggLedger.DTO.Room
{
    public class UpdateRoomPublicStatusDto
    {
        public required Guid RoomId { get; set; }
        public Guid UserId { get; set; }
        public bool IsOpen { get; set; }
    }
}
