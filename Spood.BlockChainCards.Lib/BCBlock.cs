using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Spood.BlockChainCards.Lib.Transactions;

namespace Spood.BlockChainCards.Lib;

public class BCBlock
{
    public byte[] PreviousHash { get; init; }
    public List<BCTransaction> Transactions { get; init; }
    public byte[] Hash { get; set; } = Array.Empty<byte>();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public BCBlock()
    {
        // Empty constructor for serialization
    }

    public BCBlock(byte[] previous_hash, IEnumerable<BCTransaction> transactionList)
    {
        PreviousHash = previous_hash;
        Transactions = transactionList.ToList();
        Hash = SHA256.HashData(BlockData);
    }
    
    [JsonIgnore]
    public byte[] BlockData =>
        PreviousHash
            .Concat(Transactions.SelectMany(t => t.ToSignedTransactionBytes()))
            .ToArray();

}

