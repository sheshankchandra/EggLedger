namespace EggLedger.DTO.User
{
    public class UserSummaryDto
    {
        public Guid UserId { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public int Role { get; set; }
    }
}
