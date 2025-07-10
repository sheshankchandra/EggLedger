namespace EggLedger.Models.Models
{
    public class UserRoom
    {
        public Guid Id { get; set; }
        public Guid RoomId { get; set; }
        public Guid UserId { get; set; }
        public bool IsAdmin { get; set; }
        public DateTime JoinedAt { get; set; }

        // Navigation properties
        public virtual Room Room { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
