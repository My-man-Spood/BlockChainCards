using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Spood.BlockChainCards;

public class BCTransaction
{
    public byte[] User1 { get; init; }
    public byte[] User2 { get; init; }
    public IEnumerable<byte[]> CardsFromUser1 { get; init; }
    public IEnumerable<byte[]> CardsFromUser2 { get; init; }
    public DateTime Timestamp { get; init; }
    public BCTransactionType Type { get; init; }

    private byte[]? _signature = null;

    [JsonIgnore]
    public bool IsSigned => _signature != null;

    [JsonIgnore]
    public byte[]? Signature => _signature;

    public string? Signature64
    {
        get => _signature != null ? Convert.ToBase64String(_signature) : null;
        init => _signature = value != null ? Convert.FromBase64String(value) : null;
    }

    public BCTransaction()
    {
        // Empty constructor for serialization
    }

    public BCTransaction(byte[] user1, byte[] user2, IEnumerable<byte[]> cardsFromUser1, IEnumerable<byte[]> cardsFromUser2, DateTime timestamp, BCTransactionType type)
    {
        User1 = user1;
        User2 = user2;
        CardsFromUser1 = cardsFromUser1;
        CardsFromUser2 = cardsFromUser2;
        Timestamp = timestamp;
        Type = type;
    }

    public string ToTransacionString()
    {
        var user1Hex = BitConverter.ToString(User1).Replace("-", "");
        var user2Hex = BitConverter.ToString(User2).Replace("-", "");
        var cardsFromUser1Hex = string.Join(",", CardsFromUser1.Select(card => BitConverter.ToString(card).Replace("-", "")));
        var cardsFromUser2Hex = string.Join(",", CardsFromUser2.Select(card => BitConverter.ToString(card).Replace("-", "")));
        return $"{user1Hex}:{user2Hex}:{cardsFromUser1Hex}:{cardsFromUser2Hex}:{Timestamp:O}:{Type}";
    }

    public string ToSignedTransactionString()
    {
        if (!IsSigned)
        {
            throw new InvalidOperationException("Transaction must be signed before converting to string with signature.");
        }

        return $"{ToTransacionString()}:{Convert.ToBase64String(_signature)}";
    }

    public byte[] ToSignedTransactionBytes()
    {
        if (!IsSigned)
        {
            throw new InvalidOperationException("Transaction must be signed before converting to bytes with signature.");
        }

        var transactionString = ToSignedTransactionString();
        return Encoding.UTF8.GetBytes(transactionString);
    }

    public void Sign(byte[] privateKey)
    {
        if (IsSigned)
        {
            throw new InvalidOperationException("Transaction is already signed.");
        }

        var transactionString = ToTransacionString();
        using var excdsa = ECDsa.Create();
        excdsa.ImportECPrivateKey(privateKey, out _);
        _signature = excdsa.SignData(Encoding.UTF8.GetBytes(transactionString), HashAlgorithmName.SHA256);
    }
}

public enum BCTransactionType
{
    MintCard,
    TransferCard,
    BurnCard
}
