namespace EggLedger.DTO.Stats;

/// <summary>
/// One point on the stats timeline - a day, month, or year depending on the requested range.
/// </summary>
public class StatsBucketDto
{
    public required DateTime BucketStart { get; set; }
    public required string Label { get; set; }
    public required int EggsConsumed { get; set; }
}
