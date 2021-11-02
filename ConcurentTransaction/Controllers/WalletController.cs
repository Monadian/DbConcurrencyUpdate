using CocurentTransaction.Db;
using CocurentTransaction.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static ConcurentTransaction.Helper;

namespace CocurentTransaction.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WalletController : ControllerBase
    {
        private readonly WalletContext walletContext;
        

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
                //var newBalanceType1 = await RechargeCoreAsync();

                // 2. Same as (1) but with retry
                // We try to update for 5 times (with delay), if it still conflicted just admit it.
                //var newBalanceType2 = await RechargeCoreAsync(true);

                // 3. Use DB row-lock
                var newBalanceType3 = await UseRowLockTransactionAsync(() => RechargeCoreAsync());

                return Ok(newBalanceType3);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict();
            }

            async Task<T> UseRowLockTransactionAsync<T>(Func<Task<T>> dbCommandAsync)
            {
                // The default serializable level is not enough to deal with concurrency update
                // Other transaction can still access the same row
                using var tx = await walletContext.Database.BeginTransactionAsync();

                // If you really want to deal with concurency with lock, use DB specific row-lock commnad
                // We are use SQLServer here.
                // Using UPDLOCK to Avoid a SQL Server Deadlock
                // https://www.mssqltips.com/sqlservertip/6290/sql-server-update-lock-and-updlock-table-hints/
                await walletContext.Database.ExecuteSqlRawAsync(
                    $"SELECT * FROM Wallets WITH (UPDLOCK) WHERE UserId = '{userId}'");

                var dbResult = await dbCommandAsync();

                await tx.CommitAsync();

                return dbResult;
            }

            async Task<decimal> RechargeCoreAsync(bool retry = false)
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

                var _ = retry
                    ? // Save changes with concurrency conflicts handle and retry
                      await RetryAsync<int, DbUpdateConcurrencyException>(
                        5,
                        10,
                        () => walletContext.SaveChangesAsync(),
                        static ex => HandleConcurrencyConflicts(ex.Entries))
                    : await walletContext.SaveChangesAsync();

                return entityEntry.Entity.Balance;
            }

            static void HandleConcurrencyConflicts(IEnumerable<EntityEntry> entries)
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
            }
        }
    }
}
