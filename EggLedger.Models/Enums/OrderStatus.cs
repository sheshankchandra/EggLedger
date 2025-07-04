namespace EggLedger.Models.Enums;

public enum OrderStatus
{
    Entered = 100,
    Pending = 200,
    Processing = 300,
    Retry = 400,
    Completed = 600,
    Cancelled = 700
}