using CocurentTransaction.Models;
using Microsoft.EntityFrameworkCore;
using System;

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

            modelBuilder.Entity<Wallet>()
                .HasData(
                    new Wallet(
                        Id: Guid.Parse("6816BD4E-296F-414D-A196-2833D1A980D7"),
                        UserId: Guid.Parse("05FBAAD2-994A-4F5F-9FF6-AD5023514FB3"),
                        Amount: 0,
                        Balance: 0),
                    new Wallet(
                        Id: Guid.Parse("FB3D9286-C971-4A6E-9028-59493FF143FC"),
                        UserId: Guid.Parse("8A36DD49-E7AF-4957-8707-DAD7E06E8DC7"),
                        Amount: 0,
                        Balance: 0));
        }
    }
}
