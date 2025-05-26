using EggLedger.API.Data;
using EggLedger.Core.DTOs;
using EggLedger.Core.Interfaces;
using EggLedger.Core.Models;

namespace EggLedger.API.Services
{
    public class EggTransactionService(ApplicationDbContext context) : IEggTransactionService
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<EggTransaction> AddTransactionAsync(EggTransactionDto dto)
        {
            var transaction = new EggTransaction
            {
                UserId = dto.UserId,
                Quantity = dto.Quantity,
                Date = dto.Date,
                Type = dto.Type,
                PricePerEgg = dto.PricePerEgg
            };

            context.EggTransactions.Add(transaction);
            await context.SaveChangesAsync();
            return transaction;
        }
    }
}
