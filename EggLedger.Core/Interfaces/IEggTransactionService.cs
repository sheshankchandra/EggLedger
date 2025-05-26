using EggLedger.Core.DTOs;
using EggLedger.Core.Models;

namespace EggLedger.Core.Interfaces
{
    public interface IEggTransactionService
    {
        Task<EggTransaction> AddTransactionAsync(EggTransactionDto dto);
    }
}
