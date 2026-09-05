using EggLedger.Models.Enums;

namespace EggLedger.Models.Models;

public class UserRoom
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public Guid UserId { get; set; }
    public bool IsAdmin { get; set; }
    public DateTime JoinedAt { get; set; }

    /// <summary>
    /// Approved members have full access to the room. Pending members joined a Private room and
    /// are waiting for an admin to approve them - RoomMemberHandler only treats Approved as
    /// actual membership. Public rooms create Approved rows immediately (no approval step).
    /// </summary>
    public UserRoomStatus Status { get; set; } = UserRoomStatus.Approved;

    // Navigation properties
    public virtual Room Room { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
