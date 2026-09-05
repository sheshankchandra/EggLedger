using EggLedger.Models.Enums;

namespace EggLedger.DTO.Activity;

/// <summary>
/// One event in a room's activity feed. Deliberately structured rather than a pre-rendered
/// sentence: resource nouns ("eggs", "batches") are a presentation concern owned by the
/// frontend's resource.config.js, not something the backend should bake into English text.
/// </summary>
public class ActivityEventDto
{
    public required ActivityEventType EventType { get; set; }
    public required DateTime Timestamp { get; set; }

    /// <summary>Who stocked/consumed/paid/joined.</summary>
    public required string ActorName { get; set; }

    /// <summary>The settlement receiver, only set when EventType is Settlement.</summary>
    public string? CounterpartyName { get; set; }

    /// <summary>The container involved, only set for Stock/Consume events.</summary>
    public string? ContainerName { get; set; }

    /// <summary>Units stocked/consumed, only set for Stock/Consume events.</summary>
    public int? Quantity { get; set; }

    /// <summary>Cost (Stock) or amount paid (Settlement); unset for other event types.</summary>
    public decimal? Amount { get; set; }
}
