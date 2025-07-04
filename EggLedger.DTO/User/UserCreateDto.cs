namespace EggLedger.DTO.User
{
    public class UserCreateDto
    {
        public string FirstName { get; set; } = null!;
        public string? LastName { get; set; }
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public int Role { get; set; }
    }
}
