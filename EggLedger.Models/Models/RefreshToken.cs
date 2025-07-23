namespace EggLedger.Models.Models
{
    public class RefreshToken
    {
        public RefreshToken()
        {
        }

        #region Basic Properties
        public required Guid Id { get; set; } = Guid.NewGuid(); // Foreign Key : User
        public required Guid UserId { get; set; }
        public required string Token { get; set; } = null!;
        public required DateTime Expires { get; set; }
        public required DateTime Created { get; set; }
        public string? CreatedByIp { get; set; }
        public DateTime? Revoked { get; set; }
        public string? RevokedByIp { get; set; }
        public string? ReplacedByToken { get; set; }
        #endregion

        #region Computed Properties
        /// <summary>
        /// Indicates whether the token has been revoked
        /// </summary>
        public bool IsRevoked => Revoked != null;
        #endregion

        #region Navigation Properties
        /// <summary>
        /// User associated with this refresh token
        /// </summary>
        public User User { get; set; } = null!;
        #endregion
    }
}