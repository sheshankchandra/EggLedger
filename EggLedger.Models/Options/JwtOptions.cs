using System.ComponentModel.DataAnnotations;

namespace EggLedger.Models.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    
    [Required(ErrorMessage = "JWT SecretKey is required")]
    [MinLength(32, ErrorMessage = "JWT SecretKey must be at least 32 characters long")]
    public string SecretKey { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "JWT Issuer is required")]
    public string Issuer { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "JWT Audience is required")]
    public string Audience { get; set; } = string.Empty;
    
    [Range(1, 1440, ErrorMessage = "JWT ExpiryInMinutes must be between 1 and 1440 minutes")]
    public int ExpiryInMinutes { get; set; } = 15;
}