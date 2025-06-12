using System.Security.Cryptography;
using Spood.BlockChainCards.Lib.Transactions;

namespace Spood.BlockChainCards.Lib;

public class BCBlock
{
    public byte[] PreviousHash { get; init; }
    public List<BCTransaction> Transactions { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public BCBlock()
    {
        // Empty constructor for serialization
    }

    public BCBlock(byte[] previous_hash, IEnumerable<BCTransaction> transactionList)
    {
        PreviousHash = previous_hash;
        Transactions = transactionList.ToList();
    }

    public byte[] Hash => SHA256.HashData(BlockData);
    
    public byte[] BlockData =>
        PreviousHash
            .Concat(Transactions.SelectMany(t => t.ToSignedTransactionBytes()))
            .ToArray();

}

