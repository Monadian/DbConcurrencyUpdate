using CocurentTransaction.Models;
using Microsoft.EntityFrameworkCore;

namespace CocurentTransaction.Db
{
    public class WalletContext : DbContext
    {
        public DbSet<Wallet> Wallets { get; set; }

        public WalletContext(DbContextOptions<WalletContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Wallet>()
               .Property(a => a.RowVersion)
               .IsConcurrencyToken()
               .ValueGeneratedOnAddOrUpdate();
        }
    }
}
