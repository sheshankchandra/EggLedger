namespace EggLedger.Models.Models
{
    /// <summary>
    /// Base class for entities that require audit tracking
    /// Provides comprehensive audit trail for create, update, and delete operations
    /// </summary>
    public abstract class AuditableEntity
    {
        /// <summary>
        /// When the entity was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Who created the entity
        /// </summary>
        public Guid CreatedBy { get; set; }

        /// <summary>
        /// When the entity was last modified (null if never modified)
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// Who last modified the entity (null if never modified)
        /// </summary>
        public Guid? ModifiedBy { get; set; }

        /// <summary>
        /// When the entity was deleted/archived (null if active)
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// Who deleted/archived the entity (null if active)
        /// </summary>
        public Guid? DeletedBy { get; set; }

        /// <summary>
        /// Reason for deletion/archiving (optional)
        /// </summary>
        public string? DeletionReason { get; set; }

        /// <summary>
        /// Additional audit notes (optional)
        /// </summary>
        public string? AuditNotes { get; set; }
    }
}