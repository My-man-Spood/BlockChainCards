using Spood.BlockChainCards.Lib.Utils;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace Spood.BlockChainCards.Lib.Transactions;

public class MintCardTransaction : BCTransaction
{
    [JsonConverter(typeof(HexStringJsonConverter))]
    public byte[] AuthorityPublicKey { get; init; }

    [JsonConverter(typeof(HexStringJsonConverter))]
    public byte[] RecipientPublicKey { get; init; }

    [JsonConverter(typeof(HexStringJsonConverter))]
    public byte[] Card { get; init; }

    private byte[]? _authoritySignature;
    public byte[]? AuthoritySignature => _authoritySignature;

    public override bool IsFullySigned => _authoritySignature != null;

    public override string? Id => IsFullySigned ? ToSignedTransactionBytes().ToHex() : null;

    public MintCardTransaction(byte[] authorityPublicKey, byte[] recipientPublicKey, byte[] card, DateTime timestamp)
    {
        AuthorityPublicKey = authorityPublicKey;
        RecipientPublicKey = recipientPublicKey;
        Card = card;
        Timestamp = timestamp;
    }

    public MintCardTransaction()
    {
        // for deserialization
    }

    public override string ToTransactionString()
    {
        var authorityHex = AuthorityPublicKey.ToHex();
        var recipientHex = RecipientPublicKey.ToHex();
        var cardHex = Card.ToHex();
        return $"{authorityHex}:{recipientHex}:{cardHex}:{Timestamp:O}";
    }

    public override void Sign(byte[] privateKey)
    {
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportECPrivateKey(privateKey, out _);
        var transactionString = ToTransactionString();
        _authoritySignature = ecdsa.SignData(Encoding.UTF8.GetBytes(transactionString), HashAlgorithmName.SHA256);
    }

    public override byte[] ToSignedTransactionBytes()
    {
        if (!IsFullySigned)
            throw new InvalidOperationException("Transaction must be signed before getting bytes.");
        var transactionString = ToTransactionString();
        var signature = AuthoritySignature ?? Array.Empty<byte>();
        return Encoding.UTF8.GetBytes($"{transactionString}:{Convert.ToBase64String(signature)}");
    }

    public override bool VerifySignature()
    {
        if (AuthoritySignature == null) return false;

        using var ecdsa = ECDsa.Create();
        ecdsa.ImportSubjectPublicKeyInfo(AuthorityPublicKey, out _);
        var transactionString = ToTransactionString();
        return ecdsa.VerifyData(Encoding.UTF8.GetBytes(transactionString), AuthoritySignature, HashAlgorithmName.SHA256);
    }

    public override IEnumerable<byte[]> GetAllCards()
    {
        return [Card]; 
    }
}
