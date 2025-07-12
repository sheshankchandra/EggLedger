using EggLedger.Models.Enums;

namespace EggLedger.Models.Models
{
    public class Room : AuditableEntity
    {
        public required Guid RoomId { get; set; }
        public required string RoomName { get; set; }
        public required int RoomCode { get; set; }
        public required bool IsPublic { get; set; }
        
        /// <summary>
        /// Status of the room (Active, Archived, Suspended)
        /// </summary>
        public RoomStatus Status { get; set; } = RoomStatus.Active;

        // Navigation properties
        public virtual ICollection<UserRoom> UserRooms { get; set; } = new List<UserRoom>();
        public virtual ICollection<Container> Containers { get; set; } = new List<Container>();

        /// <summary>
        /// Checks if the room is currently active and accessible
        /// </summary>
        public bool IsActive => Status == RoomStatus.Active;

        /// <summary>
        /// Checks if the room is archived (soft deleted)
        /// </summary>
        public bool IsArchived => Status == RoomStatus.Archived;
    }
}
