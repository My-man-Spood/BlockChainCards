using System.Security.Cryptography;
using System.Text;

namespace Spood.BlockChainCards.Transactions;

public class MintCardTransaction : BCTransaction
{
    public byte[] AuthorityPublicKey { get; init; }
    public byte[] RecipientPublicKey { get; init; }
    public byte[] Card { get; init; }

    private byte[]? _authoritySignature;
    public byte[]? AuthoritySignature => _authoritySignature;

    public override bool IsFullySigned => _authoritySignature != null;

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
        var authorityHex = BitConverter.ToString(AuthorityPublicKey).Replace("-", "");
        var recipientHex = BitConverter.ToString(RecipientPublicKey).Replace("-", "");
        var cardHex = BitConverter.ToString(Card).Replace("-", "");
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
}
