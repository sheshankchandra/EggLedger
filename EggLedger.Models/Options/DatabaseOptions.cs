using System.ComponentModel.DataAnnotations;

namespace EggLedger.Models.Options;

public class DatabaseOptions
{
    public const string SectionName = "ConnectionStrings";
    
    [Required(ErrorMessage = "Database DefaultConnection is required")]
    public string DefaultConnection { get; set; } = string.Empty;
}