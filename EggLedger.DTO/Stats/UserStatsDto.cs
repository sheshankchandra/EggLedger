namespace EggLedger.DTO.Stats;

/// <summary>
/// Gamified consumption stats for a user: totals for the requested range, a streak (computed
/// from all-time history regardless of range), and a time-bucketed series for charting.
/// </summary>
public class UserStatsDto
{
    public required int TotalEggsConsumed { get; set; }
    public required decimal TotalProteinGrams { get; set; }
    public required decimal TotalCalories { get; set; }
    public required int CurrentStreakDays { get; set; }
    public required int LongestStreakDays { get; set; }
    public required List<StatsBucketDto> Buckets { get; set; }
}
