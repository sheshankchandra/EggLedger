namespace EggLedger.Models.Models;

/// <summary>
/// A category of shared household resource the app can track (e.g. Eggs). Today the app is
/// still single-resource end to end and only ever seeds/uses <see cref="EggsId"/>; this table
/// exists purely as forward-compatible groundwork so a future resource type can be introduced
/// as a data change instead of another schema migration.
/// </summary>
public class ResourceType
{
    /// <summary>
    /// Well-known id of the single resource type seeded today. Existing code can reference
    /// this constant instead of querying for "the eggs row" by name.
    /// </summary>
    public static readonly Guid EggsId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public Guid ResourceTypeId { get; set; }

    /// <summary>
    /// Stable internal key (e.g. "eggs"). Not shown to users.
    /// </summary>
    public required string Name { get; set; }

    public required string DisplayName { get; set; }
    public required string Singular { get; set; }
    public required string Plural { get; set; }
    public required string InventorySingular { get; set; }
    public required string InventoryPlural { get; set; }
    public required string Icon { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Container> Containers { get; set; } = new List<Container>();
}
