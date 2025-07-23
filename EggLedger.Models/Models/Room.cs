using EggLedger.Models.Enums;

namespace EggLedger.Models.Models
{
    public class Room : AuditableEntity
    {
        public Room()
        {
        }

        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required int Code { get; set; }
        public required bool IsPublic { get; set; }
        
        /// <summary>
        /// Status of the room (Active, Archived, Suspended)
        /// </summary>
        public RoomStatus Status { get; set; } = RoomStatus.Active;

        // get-only properties

        /// <summary>
        /// Checks if the room is currently active and accessible
        /// </summary>
        public bool IsActive => Status == RoomStatus.Active;

        /// <summary>
        /// Checks if the room is archived (soft deleted)
        /// </summary>
        public bool IsArchived => Status == RoomStatus.Archived;

        // Navigation properties
        public virtual ICollection<UserRoom> UserRooms { get; set; } = new List<UserRoom>();
        public virtual ICollection<Container> Containers { get; set; } = new List<Container>();
    }
}
