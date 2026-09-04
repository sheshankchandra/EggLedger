using System.ComponentModel.DataAnnotations;

namespace EggLedger.Models.Options;

/// <summary>
/// Per-unit nutrition values used to turn a raw consumption count into gamified stats
/// (protein, calories). Defaults are for one large egg. When multi-resource support lands,
/// this becomes per-resource-type instead of a single global value.
/// </summary>
public class NutritionOptions
{
    public const string SectionName = "Nutrition";

    [Range(0, double.MaxValue, ErrorMessage = "CaloriesPerUnit must not be negative")]
    public decimal CaloriesPerUnit { get; set; } = 78m;

    [Range(0, double.MaxValue, ErrorMessage = "ProteinGramsPerUnit must not be negative")]
    public decimal ProteinGramsPerUnit { get; set; } = 6.3m;
}
