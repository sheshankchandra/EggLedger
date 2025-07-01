namespace EggLedger.Core.DTOs.Room
{
    public class RoomDto
    {
        public required Guid RoomId { get; set; }
        public Guid? AdminUserId { get; set; }
        public string? RoomName { get; set; }
        public int? RoomCode { get; set; }
        public bool? IsOpen { get; set; }
    }
}
