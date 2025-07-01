namespace EggLedger.Core.Models
{
    public class Room
    {
        public required Guid RoomId { get; set; }
        public required string RoomName { get; set; }
        public required int RoomCode { get; set; }
        public required bool IsPublic { get; set; }
        public required DateTime CreatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<UserRoom> UserRooms { get; set; } = new List<UserRoom>();
        public virtual ICollection<Container> Containers { get; set; } = new List<Container>();
    }
}
