using System;

namespace CocurentTransaction.Models
{
    public record Wallet(Guid Id, Guid UserId, decimal Amount, decimal Balance)
    {
        public byte[] RowVersion { get; init; }

        private Wallet() 
            : this(Guid.Empty, Guid.Empty, 0, 0)
        {
        }
    }
}
