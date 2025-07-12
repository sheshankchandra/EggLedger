using System.ComponentModel.DataAnnotations;

namespace EggLedger.Models.Options;

public class GoogleAuthOptions
{
    public const string SectionName = "Authentication:Google";
    
    [Required(ErrorMessage = "Google ClientId is required")]
    public string ClientId { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Google ClientSecret is required")]
    public string ClientSecret { get; set; } = string.Empty;
}