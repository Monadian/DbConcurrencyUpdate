using System;

namespace ConcurrentTransaction.Models.Messages
{
    public interface RechargeWallet
    {
        public Guid UserId { get; }
        public decimal Amount { get; }
    }

    public interface RechargeWalletResult
    {
        public Guid UserId { get; }

        public decimal Balance { get; }
    }
}
