namespace EggLedger.Models.Models
{
    public class User
    {
        public User()
        {
        }

        #region Basic Properties
        public required Guid Id { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }
        public required int Role { get; set; }
        /// <summary>
        /// Authentication provider (e.g., "Google", "Local")
        /// </summary>
        public string? Provider { get; set; }
        #endregion

        #region Computed Properties
        /// <summary>
        /// Full name of the user
        /// </summary>
        public string Name => $"{FirstName} {LastName}".Trim();
        #endregion

        #region Navigation Properties
        /// <summary>
        /// User's password information (null for external auth providers)
        /// </summary>
        public virtual UserPassword? UserPassword { get; set; }

        /// <summary>
        /// Orders created by this user
        /// </summary>
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

        /// <summary>
        /// Transactions associated with this user
        /// </summary>
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

        /// <summary>
        /// Containers purchased by this user
        /// </summary>
        public virtual ICollection<Container> Containers { get; set; } = new List<Container>();

        /// <summary>
        /// Refresh tokens for this user's sessions
        /// </summary>
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

        /// <summary>
        /// User's room memberships
        /// </summary>
        public virtual ICollection<UserRoom> UserRooms { get; set; } = new List<UserRoom>();
        #endregion
    }
}
