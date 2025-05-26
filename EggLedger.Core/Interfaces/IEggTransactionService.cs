using EggLedger.Core.DTOs;
using EggLedger.Core.Models;

namespace EggLedger.Core.Interfaces
{
    public interface IEggTransactionService
    {
        Task<Transaction> StockEggTransactionAsync(EggTransactionDto dto);
        Task<Transaction> ConsumeEggTransactionAsync(EggTransactionDto dto);
    }
}
