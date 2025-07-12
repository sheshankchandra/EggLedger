namespace EggLedger.Models.Enums;

public enum ContainerStatus
{
    Available = 1,
    Depleted = 2,
    Archived = 3,    // "Deleted" but preserved
    Suspended = 4
}