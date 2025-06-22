using EggLedger.API.Data;
using EggLedger.Core.DTOs.Container;
using EggLedger.Core.Interfaces;
using EggLedger.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace EggLedger.API.Services
{
    public class ContianerService(ApplicationDbContext context) : IContainerService
    {
        public async Task<List<ContainerSummaryDto>> GetContainersAsync()
        {
            IQueryable<ContainerSummaryDto> query = context.Containers
                .Where(c => c.RemainingQuantity > 0)
                .Include(c => c.Buyer) // Still needed if Buyer.Name is accessed in Select
                .OrderBy(c => c.PurchaseDateTime)
                .Select(c => new ContainerSummaryDto // This projection happens on the database server
                {
                    ContainerId = c.ContainerId,
                    ContainerName = c.ContainerName,
                    RemainingQuantity = c.RemainingQuantity,
                    TotalQuantity = c.TotalQuantity,
                    OwnerName = c.Buyer.Name // Accessing Buyer.Name requires the Include above
                });

            List<ContainerSummaryDto> result = await query.ToListAsync();

            return result;
        }
    }
}
