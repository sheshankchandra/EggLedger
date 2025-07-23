namespace EggLedger.Models.Models
{
    public class UserRoom
    {
        public UserRoom()
        {
        }

        #region Basic Properties
        public required Guid Id { get; set; } // Foreign Key : Room
        public required Guid RoomId { get; set; } // Foreign Key : User
        public required Guid UserId { get; set; }
        public required bool IsAdmin { get; set; }
        public required DateTime JoinedAt { get; set; }
        #endregion

        #region Navigation Properties
        /// <summary>
        /// Room the user is a member of
        /// </summary>
        public virtual Room Room { get; set; } = null!;

        /// <summary>
        /// User who is a member of the room
        /// </summary>
        public virtual User User { get; set; } = null!;
        #endregion
    }
}
