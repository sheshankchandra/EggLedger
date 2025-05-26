using EggLedger.API.Data;
using EggLedger.Core.DTOs;
using EggLedger.Core.Interfaces;
using EggLedger.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace EggLedger.API.Services
{
    public class EggTransactionService(ApplicationDbContext context) : IEggTransactionService
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<Transaction> StockEggTransactionAsync(EggTransactionDto dto)
        {
            var transaction = new Transaction
            {
                UserId = dto.UserId,
                Quantity = dto.Quantity,
                Date = dto.Date,
                Type = dto.Type,
                PricePerEgg = Math.Round(dto.Amount / dto.Quantity, 2)
            };

            var content = new Content
            {
                Id = Guid.NewGuid(),
                BroughtByUserId = dto.UserId,
                TotalEggs = dto.Quantity,
                RemainingEggs = dto.Quantity,
                Amount = dto.Amount,
                PurchaseDateTime = dto.Date,
            };

            _context.Contents.Add(content);
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<Transaction> ConsumeEggTransactionAsync(EggTransactionDto dto)
        {
            var remainingPick = dto.Quantity;
            var transactionId = Guid.NewGuid();

            var availableContents = await _context.Contents
                .Where(c => c.RemainingEggs > 0)
                .OrderBy(c => c.PurchaseDateTime)
                .ToListAsync();

            foreach (var content in availableContents)
            {
                if (remainingPick <= 0)
                {
                    break;
                }
                if (content.RemainingEggs <= 0)
                {
                    continue;
                }

                int taken = Math.Min(remainingPick, content.RemainingEggs);

                var detail = new TransactionDetail()
                {
                    Id = Guid.NewGuid(),
                    TransactionId = transactionId,
                    ContentId = content.Id,
                    EggsTakenFromContent = taken
                };

                content.RemainingEggs -= taken;
                remainingPick -= taken;

                _context.TransactionDetails.AddAsync(detail);
            }

            if (remainingPick > 0)
            {
                throw new InvalidOperationException("Not enough eggs in stock to fulfill this consumption.");
            }

            var transaction = new Transaction
            {
                UserId = dto.UserId,
                Quantity = dto.Quantity,
                Date = dto.Date,
                Type = dto.Type,
                PricePerEgg = Math.Round(dto.Amount / dto.Quantity, 2)
            };

            await _context.SaveChangesAsync();
            return transaction;
        }
    }
}
