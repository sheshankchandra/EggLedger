namespace EggLedger.Core.Constants;

public enum OrderDetailStatus
{
    Entered = 101,
    Pending = 201,
    Processing = 301,
    Retry = 401,
    Completed = 601,
    Cancelled = 701
}