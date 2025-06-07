using System.Security.Cryptography;
using System.Text;

namespace Spood.BlockChainCards.Transactions;

public class TradeCardsTransaction : BCTransaction
{
    public byte[] User1PublicKey { get; set; }
    public byte[] User2PublicKey { get; set; }
    public IEnumerable<byte[]> CardsFromUser1 { get; set; }
    public IEnumerable<byte[]> CardsFromUser2 { get; set; }

    private byte[]? _user1Signature;
    private byte[]? _user2Signature;
    public byte[]? User1Signature => _user1Signature;
    public byte[]? User2Signature => _user2Signature;

    public override bool IsFullySigned => _user1Signature != null && _user2Signature != null;

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
        var user1Hex = BitConverter.ToString(User1PublicKey).Replace("-", "");
        var user2Hex = BitConverter.ToString(User2PublicKey).Replace("-", "");
        var cardsFromUser1Hex = string.Join(",", CardsFromUser1.Select(card => BitConverter.ToString(card).Replace("-", "")));
        var cardsFromUser2Hex = string.Join(",", CardsFromUser2.Select(card => BitConverter.ToString(card).Replace("-", "")));
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
}
