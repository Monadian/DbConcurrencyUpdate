using System;
using System.Threading.Tasks;

namespace ConcurentTransaction
{
    public static class Helper
    {
        private static readonly Random random = new();
        private static int MaximumRetryDelay { get; set; } = 500;
        private static float RetryDelayMultiplier { get; set; } = 2;

        public static async Task<T> RetryAsync<T, TException>(
                int retryLimit,
                int millisecondsDelay,
                Func<Task<T>> funcAsync,
                Action<TException> exceptionHandler)
                where TException : Exception
        {
            try
            {
                return await funcAsync();
            }
            catch (TException ex)
            {
                if (retryLimit < 0)
                    throw;  // we reach the maximum try, let's give up

                exceptionHandler(ex);

                var intervalRandomnessValue = random.Next(-millisecondsDelay, millisecondsDelay);
                await Task.Delay(millisecondsDelay + intervalRandomnessValue); // Delay before we try again

                var newDelay = (int)(millisecondsDelay * RetryDelayMultiplier);

                // Let's try again with a longer delay
                return await RetryAsync(
                    retryLimit - 1,
                    newDelay > MaximumRetryDelay ? MaximumRetryDelay : newDelay,
                    funcAsync,
                    exceptionHandler);
            }
        }
    }
}
