using Spood.BlockChainCards.Lib.Utils;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace Spood.BlockChainCards.Lib.Transactions;

public class TradeCardsTransaction : BCTransaction
{
    [JsonConverter(typeof(HexStringJsonConverter))]
    public byte[] User1PublicKey { get; set; }
    [JsonConverter(typeof(HexStringJsonConverter))]
    public byte[] User2PublicKey { get; set; }
    public IEnumerable<byte[]> CardsFromUser1 { get; set; }
    public IEnumerable<byte[]> CardsFromUser2 { get; set; }

    private byte[]? _user1Signature;
    private byte[]? _user2Signature;
    public byte[]? User1Signature => _user1Signature;
    public byte[]? User2Signature => _user2Signature;

    public override bool IsFullySigned => _user1Signature != null && _user2Signature != null;

    public override string? Id => IsFullySigned ? ToSignedTransactionBytes().ToHex(): null;

    public TradeCardsTransaction(byte[] user1PublicKey, byte[] user2PublicKey, IEnumerable<byte[]> cardsFromUser1, IEnumerable<byte[]> cardsFromUser2, DateTime timestamp)
    {
        User1PublicKey = user1PublicKey;
        User2PublicKey = user2PublicKey;
        CardsFromUser1 = cardsFromUser1;
        CardsFromUser2 = cardsFromUser2;
        Timestamp = timestamp;
    }

    public TradeCardsTransaction()
    {
        // for deserialization
    }

    public override string ToTransactionString()
    {
        var user1Hex = User1PublicKey.ToHex();
        var user2Hex = User2PublicKey.ToHex();
        var cardsFromUser1Hex = string.Join(",", CardsFromUser1.Select(card => card.ToHex()));
        var cardsFromUser2Hex = string.Join(",", CardsFromUser2.Select(card => card.ToHex()));
        return $"{user1Hex}:{user2Hex}:{cardsFromUser1Hex}:{cardsFromUser2Hex}:{Timestamp:O}";
    }

    /// <summary>
    /// Both parties sign the transaction with this method
    /// </summary>
    public override void Sign(byte[] privateKey)
    {
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportECPrivateKey(privateKey, out _);
        var transactionString = ToTransactionString();
        var signature = ecdsa.SignData(Encoding.UTF8.GetBytes(transactionString), HashAlgorithmName.SHA256);

        // Try to match the public key
        var pubKey = ecdsa.ExportSubjectPublicKeyInfo();
        if (User1PublicKey.SequenceEqual(pubKey))
            _user1Signature = signature;
        else if (User2PublicKey.SequenceEqual(pubKey))
            _user2Signature = signature;
        else
            throw new InvalidOperationException("Private key does not match either user.");
    }

    public override byte[] ToSignedTransactionBytes()
    {
        if (!IsFullySigned)
            throw new InvalidOperationException("Transaction must be signed by both parties before getting bytes.");
        var transactionString = ToTransactionString();
        var sig1 = User1Signature ?? Array.Empty<byte>();
        var sig2 = User2Signature ?? Array.Empty<byte>();
        return Encoding.UTF8.GetBytes($"{transactionString}:{Convert.ToBase64String(sig1)}:{Convert.ToBase64String(sig2)}");
    }

    public override bool VerifySignature()
    {
        if(!IsFullySigned) return false;

        return VerifyUserSignature(User1Signature!, User1PublicKey)
            && VerifyUserSignature(User2Signature!, User2PublicKey);
    }

    private bool VerifyUserSignature(byte[] signature, byte[] publicKey)
    {
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportSubjectPublicKeyInfo(publicKey, out _);
        return ecdsa.VerifyData(Encoding.UTF8.GetBytes(ToTransactionString()), signature, HashAlgorithmName.SHA256);
    }

    public override IEnumerable<byte[]> GetAllCards()
    {
        return CardsFromUser1.Concat(CardsFromUser2);
    }
}
