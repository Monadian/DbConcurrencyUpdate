using CocurentTransaction.Db;
using CocurentTransaction.Models;
using ConcurrentTransaction.Models.Messages;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static ConcurentTransaction.Helper;

namespace ConcurrentTransaction.Consumers
{
    public class RechargeWalletConsumer :
        IConsumer<RechargeWallet>
    {
        private readonly WalletContext walletContext;

        public RechargeWalletConsumer(WalletContext walletContext)
        {
            this.walletContext = walletContext;
        }

        public async Task Consume(ConsumeContext<RechargeWallet> context)
        {
            var newBalanceAmount = await RechargeCoreAsync();

            await context.RespondAsync<RechargeWalletResult>(new
            {
                UserId = context.Message.UserId,
                Balance = newBalanceAmount,
            });

            async Task<decimal> RechargeCoreAsync()
            {
                var wallet = await walletContext.Wallets
                    .AsNoTracking()
                    .SingleOrDefaultAsync(w => w.UserId == context.Message.UserId);

                var rechargedWallet = wallet with
                {
                    Amount = context.Message.Amount,
                    Balance = wallet.Balance + context.Message.Amount
                };

                var entityEntry = walletContext.Wallets.Update(rechargedWallet);

                await RetryAsync<int, DbUpdateConcurrencyException>(
                    1,
                    0,  // retry immediately
                    () => walletContext.SaveChangesAsync(),
                    static ex => HandleConcurrencyConflicts(ex.Entries));

                return entityEntry.Entity.Balance;
            }

            static bool HandleConcurrencyConflicts(IEnumerable<EntityEntry> entries)
            {
                // Code from https://docs.microsoft.com/en-us/ef/core/saving/concurrency
                foreach (var entry in entries)
                {
                    if (entry.Entity is Wallet)
                    {
                        var proposedValues = entry.CurrentValues;
                        var databaseValues = entry.GetDatabaseValues();

                        var proposedBalance = (decimal)proposedValues["Balance"];
                        var proposedAmount = (decimal)proposedValues["Amount"];
                        var databaseBalance = (decimal)databaseValues["Balance"];

                        // Add current balance with proposed amount again
                        proposedValues["Balance"] = databaseBalance + proposedAmount;

                        // Refresh original values to bypass next concurrency check
                        entry.OriginalValues.SetValues(databaseValues);
                    }
                    else
                    {
                        throw new NotSupportedException(
                            "Don't know how to handle concurrency conflicts for "
                            + entry.Metadata.Name);
                    }
                }

                return true;
            }
        }
    }

}
