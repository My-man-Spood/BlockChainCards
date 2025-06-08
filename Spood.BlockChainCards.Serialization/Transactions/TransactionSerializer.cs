using Spood.BlockChainCards.Lib.Transactions;
using System.Text.Json;

namespace Spood.BlockChainCards.Serialization.Transactions;

public static class TransactionSerializer
{
    public static byte[] Serialize(BCTransaction transaction)
    {
        switch(transaction)
        {
            case MintCardTransaction mintCardTransaction:
                return new MintCardTransactionSerializer().Serialize(mintCardTransaction);
            case TradeCardsTransaction tradeCardsTransaction:
                return new TradeCardsTransactionSerializer().Serialize(tradeCardsTransaction);
            default:
                throw new ArgumentException("Unsupported transaction type");
        }
    }

    public static T Deserialize<T>(byte[] bytes) where T : BCTransaction
    {
        return Deserialize(bytes) as T;
    }

    public static BCTransaction Deserialize(byte[] bytes)
    {
        var discriminator = bytes[1];
        switch(discriminator)
        {
            case MintCardTransactionSerializer.discriminator:
                return new MintCardTransactionSerializer().Deserialize(bytes);
            case 2:
                return new TradeCardsTransactionSerializer().Deserialize(bytes);
            default:
                throw new ArgumentException("Unsupported transaction type");
        }
    }
}