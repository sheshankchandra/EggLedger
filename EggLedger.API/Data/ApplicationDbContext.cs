using System.Collections.Generic;
using EggLedger.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace EggLedger.API.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Transaction> Transactions => Set<Transaction>();
        public DbSet<TransactionDetail> TransactionDetails => Set<TransactionDetail>();
        public DbSet<Content> Contents => Set<Content>();
    }
}
