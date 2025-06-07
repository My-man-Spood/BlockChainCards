using System.Text.Json.Serialization;

namespace Spood.BlockChainCards.Lib.Transactions;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "Type")]
[JsonDerivedType(typeof(TradeCardsTransaction), "TradeCards")]
[JsonDerivedType(typeof(MintCardTransaction), "MintCard")]
public abstract class BCTransaction
{
    public DateTime Timestamp { get; set; }

    public abstract bool IsFullySigned { get; }
    public abstract string ToTransactionString();
    public abstract void Sign(byte[] privateKey);
    public abstract byte[] ToSignedTransactionBytes();
    public abstract bool VerifySignature();
}
