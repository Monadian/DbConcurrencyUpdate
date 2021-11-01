using CocurentTransaction.Db;
using CocurentTransaction.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CocurentTransaction.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WalletController : ControllerBase
    {
        private readonly WalletContext walletContext;
        private static readonly Random random = new();

        public WalletController(WalletContext walletContext)
        {
            this.walletContext = walletContext;
        }

        [HttpPost("recharge/{userId}")]
        public async Task<IActionResult> RechargeAsync(Guid userId, [FromBody] RechargeDto model)
        {
            try
            {
                // 1. Let's try to update record with optimisic concurrency check to avoid performance penalty and deadlock
                // this line check for concurrency update and do conflict handle without retry
                //var newBalance = await RechargeCoreAsync();

                // 2. Same as (1) but with retry
                // We try to update for 5 times (with delay), if it still conflicted just admit it.
                //var newBalance = await TryConcurrentUpdateAsync(
                //    5,
                //    1000,
                //    async () => await RechargeCoreAsync());

                // 3. Use DB row-lock
                var newBalance = await UseRowLockTransactionAsync(RechargeCoreAsync);

                return Ok(newBalance);
            }
            catch (DbUpdateException)
            {
                return Conflict();
            }

            static async ValueTask<T> TryConcurrentUpdateAsync<T>(int retryCount, int interval, Func<Task<T>> asyncFunc)
            {
                try
                {
                    return await asyncFunc();
                }
                catch(DbUpdateException)
                {
                    if (retryCount == 0)
                        throw;  //we reach the maximum try, let's give up

                    var intervalRandomnessValue = random.Next(-interval / 2, (int)(interval * 1.5));
                    await Task.Delay(interval + intervalRandomnessValue); //Delay before we try again
                    return await TryConcurrentUpdateAsync(retryCount - 1, (int)(interval * 1.25), asyncFunc); //Let's try again with a longer delay
                }
            }

            async Task<T> UseRowLockTransactionAsync<T>(Func<Task<T>> dbCommand)
            {
                //The default exclusive lock is not enough to deal with concurrency update
                //Other transaction can still read the same value
                using var tx = walletContext.Database.BeginTransaction();

                //If you really want to deal with concurency with lock, use DB specific row-lock commnad
                //We are use SQLServer here
                await walletContext.Database.ExecuteSqlRawAsync(
                    $"SELECT * FROM Wallets WITH (UPDLOCK) WHERE UserId = '{userId}'");

                var dbResult = await dbCommand();

                await tx.CommitAsync();

                return dbResult;
            }

            async Task<decimal> RechargeCoreAsync()
            {
                var wallet = await walletContext.Wallets
                    .AsNoTracking()
                    .SingleOrDefaultAsync(w => w.UserId == userId);

                var rechargedWallet = wallet with 
                {
                    Amount = model.Amount,
                    Balance = wallet.Balance + model.Amount 
                };

                var entityEntry = walletContext.Wallets.Update(rechargedWallet);

                //await walletContext.SaveChangesAsync();
                await SaveChangesWithConflitHandlerAsync();

                return entityEntry.Entity.Balance;
            }

            void HandleUpdateConcurrencyUpdate(IEnumerable<EntityEntry> entries)
            {
                //Code from https://docs.microsoft.com/en-us/ef/core/saving/concurrency
                foreach (var entry in entries)
                {
                    if (entry.Entity is Wallet)
                    {
                        var proposedValues = entry.CurrentValues;
                        var databaseValues = entry.GetDatabaseValues();

                        var proposedBalance = (decimal)proposedValues["Balance"];
                        var proposedAmount = (decimal)proposedValues["Amount"];
                        var databaseBalance = (decimal)databaseValues["Balance"];

                        //Add current balance with proposed amount again
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
            }

            async Task<int> SaveChangesWithConflitHandlerAsync()
            {
                try
                {
                    return await walletContext.SaveChangesAsync();
                }
                catch (DbUpdateException ex)
                {
                    HandleUpdateConcurrencyUpdate(ex.Entries);

                    return await walletContext.SaveChangesAsync();
                }
            }
        }
    }
}
