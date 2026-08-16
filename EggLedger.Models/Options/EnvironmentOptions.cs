using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace EggLedger.Models.Options;

public class EnvironmentOptions
{
    public const string SectionName = "Environment";

    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
        Justification = "Property name intentionally matches the EGGLEDGER_LOG_PATH configuration/environment key.")]
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