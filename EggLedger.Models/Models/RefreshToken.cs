namespace EggLedger.Models.Models
{
    public class RefreshToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Token { get; set; } = null!;
        public DateTime Expires { get; set; }
        public DateTime Created { get; set; }
        public string? CreatedByIp { get; set; }

        public DateTime? Revoked { get; set; }
        public string? RevokedByIp { get; set; }
        public string? ReplacedByToken { get; set; }

        public bool IsRevoked => Revoked != null;

        // Foreign key
        public Guid UserId { get; set; }

        // Navigation property
        public User User { get; set; } = null!;
    }
}