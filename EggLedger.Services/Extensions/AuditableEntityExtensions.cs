using EggLedger.Models.Models;

namespace EggLedger.Services.Extensions
{
    /// <summary>
    /// Extension methods for working with auditable entities
    /// </summary>
    public static class AuditableEntityExtensions
    {
        /// <summary>
        /// Sets the creation audit fields for a new entity
        /// </summary>
        public static T SetCreatedBy<T>(this T entity, Guid userId, string? notes = null) where T : AuditableEntity
        {
            entity.CreatedAt = DateTime.UtcNow;
            entity.CreatedBy = userId;
            entity.AuditNotes = notes;
            return entity;
        }

        /// <summary>
        /// Sets the modification audit fields for an existing entity
        /// </summary>
        public static T SetModifiedBy<T>(this T entity, Guid userId, string? notes = null) where T : AuditableEntity
        {
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = userId;
            if (!string.IsNullOrEmpty(notes))
            {
                entity.AuditNotes = notes;
            }
            return entity;
        }

        /// <summary>
        /// Sets the deletion audit fields for an entity being archived/deleted
        /// </summary>
        public static T SetDeletedBy<T>(this T entity, Guid userId, string? reason = null, string? notes = null) where T : AuditableEntity
        {
            entity.DeletedAt = DateTime.UtcNow;
            entity.DeletedBy = userId;
            entity.DeletionReason = reason;
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = userId;
            if (!string.IsNullOrEmpty(notes))
            {
                entity.AuditNotes = notes;
            }
            return entity;
        }

        /// <summary>
        /// Checks if the entity has been deleted/archived
        /// </summary>
        public static bool IsDeleted<T>(this T entity) where T : AuditableEntity
        {
            return entity.DeletedAt.HasValue;
        }

        /// <summary>
        /// Gets the full audit trail as a formatted string
        /// </summary>
        public static string GetAuditTrail<T>(this T entity) where T : AuditableEntity
        {
            var trail = $"Created: {entity.CreatedAt:yyyy-MM-dd HH:mm:ss} by {entity.CreatedBy}";
            
            if (entity.ModifiedAt.HasValue)
            {
                trail += $"\nModified: {entity.ModifiedAt:yyyy-MM-dd HH:mm:ss} by {entity.ModifiedBy}";
            }
            
            if (entity.DeletedAt.HasValue)
            {
                trail += $"\nDeleted: {entity.DeletedAt:yyyy-MM-dd HH:mm:ss} by {entity.DeletedBy}";
                if (!string.IsNullOrEmpty(entity.DeletionReason))
                {
                    trail += $"\nReason: {entity.DeletionReason}";
                }
            }
            
            if (!string.IsNullOrEmpty(entity.AuditNotes))
            {
                trail += $"\nNotes: {entity.AuditNotes}";
            }
            
            return trail;
        }
    }
}