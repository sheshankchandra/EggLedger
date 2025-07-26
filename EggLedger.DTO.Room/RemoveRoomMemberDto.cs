namespace EggLedger.DTO.Room
{
    public class RemoveRoomMemberDto
    {
        public required Guid RoomId { get; set; }
        public required Guid MemberUserId { get; set; }
    }
}
