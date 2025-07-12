using System.ComponentModel.DataAnnotations;

namespace EggLedger.Models.Options;

public class EnvironmentOptions
{
    public const string SectionName = "Environment";
    
    public LogPathOptions EGGLEDGER_LOG_PATH { get; set; } = new();
}

public class LogPathOptions
{
    [Required]
    public string Windows { get; set; } = "C:\\Logs\\EggLedger";
    
    [Required]
    public string Linux { get; set; } = "/var/log/eggledger";
    
    [Required]
    public string macOS { get; set; } = "/var/log/eggledger";
}