namespace EggLedger.Models.Models
{
    public class Transaction
    {
        public Transaction()
        {
        }

        #region Basic Properties
        public required Guid Id { get; set; } // Foreign Key : Order
        public required Guid OrderId { get; set; }
        public required DateTime Datestamp { get; set; } // Foreign Key : User
        public required Guid PayerId { get; set; } // Foreign Key : User
        public required Guid ReceiverId { get; set; }
        public required int Amount { get; set; }
        public required int Status { get; set; }
        #endregion

        #region Navigation Properties
        /// <summary>
        /// User who is paying in the transaction
        /// </summary>
        public virtual User Payer { get; set; } = null!;

        /// <summary>
        /// User who is receiving the payment
        /// </summary>
        public virtual User Receiver { get; set; } = null!;

        /// <summary>
        /// Order associated with this transaction
        /// </summary>
        public virtual Order Order { get; set; } = null!;
        #endregion
    }
}
