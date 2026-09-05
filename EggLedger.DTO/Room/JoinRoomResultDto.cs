namespace EggLedger.DTO.Room;

public class JoinRoomResultDto
{
    public required int RoomCode { get; set; }

    /// <summary>True when the room is Private and the join is waiting on admin approval.</summary>
    public required bool IsPending { get; set; }
}
