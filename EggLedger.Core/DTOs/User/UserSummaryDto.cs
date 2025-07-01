namespace EggLedger.Core.DTOs.User
{
    public class UserSummaryDto
    {
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public int Role { get; set; }
    }
}
