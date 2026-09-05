namespace EggLedger.DTO.Room;

/// <summary>A user waiting on admin approval to join a Private room.</summary>
public class PendingMemberDto
{
    public required Guid UserId { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required DateTime RequestedAt { get; set; }
}
