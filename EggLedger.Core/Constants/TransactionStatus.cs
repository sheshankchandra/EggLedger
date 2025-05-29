namespace EggLedger.Core.Constants;

public enum TransactionStatus
{
    Entered = 102,
    Pending = 202,
    Processing = 302,
    Retry = 402,
    Completed = 602,
    Cancelled = 702
}