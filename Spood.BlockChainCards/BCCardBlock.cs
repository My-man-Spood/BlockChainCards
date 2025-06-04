using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace Spood.BlockChainCards;

public class BCCardBlock
{
    public byte[] PreviousHash { get; init; }
    public List<BCTransaction> Transactions { get; init; }

    [JsonIgnore]
    public byte[] Hash { get; set; } = Array.Empty<byte>();

    public string Hash64
    {
        get => Convert.ToBase64String(Hash);
        init => Hash = Convert.FromBase64String(value);
    }

    public BCCardBlock()
    {
        // Empty constructor for serialization
    }

    public BCCardBlock(byte[] previous_hash, IEnumerable<BCTransaction> transactionList)
    {
        PreviousHash = previous_hash;
        Transactions = transactionList.ToList();
        // Convert PreviousHash to hex string
        string previousHashHex = BitConverter.ToString(PreviousHash).Replace("-", "");
        // Combine PreviousHash and Transactions into Data

        var sha = SHA256.Create();
        Hash = sha.ComputeHash(BlockData);
    }
    
    [JsonIgnore]
    public byte[] BlockData =>
        PreviousHash
            .Concat(Transactions.SelectMany(t => t.ToSignedTransactionBytes()))
            .ToArray();

}

