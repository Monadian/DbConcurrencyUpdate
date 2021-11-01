using CocurentTransaction.Db;
using CocurentTransaction.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace CocurentTransaction.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WalletController : ControllerBase
    {
        private readonly WalletContext walletContext;
        private static readonly Random random = new Random();

        public WalletController(WalletContext walletContext)
        {
            this.walletContext = walletContext;
        }

        [HttpPost("recharge/{userId}")]
        public async Task<IActionResult> RechargeAsync(Guid userId, [FromBody]RechargeDto model)
        {
            try
            {
                //Let's try to update record with optimisic concurrency check to avoid performance penalty and deadlock
                //We try to update for 5 times (with delay), if it still conflicted just admit it.
                var newBalance = await TryConcurrentUpdateAsync(
                    5,
                    1000,
                    async () => await RechargeCoreAsync());

                return Ok(newBalance);
            }
            catch(DbUpdateException)
            {
                return Conflict();
            }

            static async ValueTask<TVal> TryConcurrentUpdateAsync<TVal>(int retryCount, int interval, Func<Task<TVal>> asyncFunc)
            {
                try
                {
                    return await asyncFunc();
                }
                catch(DbUpdateException)
                {
                    if (retryCount == 0)
                        throw;  //we reach the maximum try, let's give up

                    var intervalRandomnessValue = random.Next(-interval, interval);
                    await Task.Delay(interval + intervalRandomnessValue); //Delay before we try again
                    return await TryConcurrentUpdateAsync(retryCount - 1, (int)(interval * 1.25), asyncFunc);
                }
            }

            async Task<decimal> RechargeCoreAsync()
            {
                using var tx = walletContext.Database.BeginTransaction();

                //If you really want to deal with concurency with lock use DB specific row-lock commnad
                await walletContext.Database.ExecuteSqlRawAsync(
                    $"SELECT * FROM Wallets WITH (UPDLOCK) WHERE UserId = '{userId}'");

                var wallet = await walletContext.Wallets
                    .AsNoTracking()
                    .SingleOrDefaultAsync(w => w.UserId == userId);

                var rechargedWallet = wallet with { Balance = wallet.Balance + model.Amount };

                var entityEntry = walletContext.Wallets.Update(rechargedWallet);

                await walletContext.SaveChangesAsync();
                await tx.CommitAsync();

                return entityEntry.Entity.Balance;
            }
        }
    }
}
