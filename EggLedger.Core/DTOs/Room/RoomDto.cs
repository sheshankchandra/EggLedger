namespace EggLedger.Core.DTOs.Room
{
    public class RoomDto
    {
        public required Guid RoomId { get; set; }
        public required Guid? AdminUserId { get; set; }
        public required string? RoomName { get; set; }
        public required int? RoomCode { get; set; }
        public required bool? IsOpen { get; set; }
        public required DateTime CreateAt { get; set; }
        public required int? ContainerCount { get; set; }
        public required int? TotalEggs { get; set; }
        public required int? MemberCount { get; set; }
    }
}
