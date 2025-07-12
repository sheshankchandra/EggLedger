using System.ComponentModel.DataAnnotations;

namespace EggLedger.Models.Options;

public class CorsOptions
{
    public const string SectionName = "Cors";
    
    [Required(ErrorMessage = "CORS AllowedOrigins is required")]
    [MinLength(1, ErrorMessage = "At least one allowed origin must be specified")]
    public string[] AllowedOrigins { get; set; } = ["http://localhost:5173"];
    
    [Required(ErrorMessage = "CORS PolicyName is required")]
    public string PolicyName { get; set; } = "_myAllowSpecificOrigins";
}