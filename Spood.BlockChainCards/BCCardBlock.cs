using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Spood.BlockChainCards.Transactions;

namespace Spood.BlockChainCards;

public class BCCardBlock
{
    public byte[] PreviousHash { get; init; }
    public List<BCTransaction> Transactions { get; init; }
    public byte[] Hash { get; set; } = Array.Empty<byte>();

    public BCCardBlock()
    {
        // Empty constructor for serialization
    }

    public BCCardBlock(byte[] previous_hash, IEnumerable<BCTransaction> transactionList)
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

