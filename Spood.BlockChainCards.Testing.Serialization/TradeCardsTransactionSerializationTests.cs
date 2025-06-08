using Spood.BlockChainCards.Lib.Transactions;
using Spood.BlockChainCards.Serialization.Transactions;
using Spood.BlockChainCards.Testing.Lib.TestApi;
using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Spood.BlockChainCards.Testing.Serialization;

public class ByteArrayComparer : IEqualityComparer<byte[]>
{
    public bool Equals(byte[] x, byte[] y) => x.SequenceEqual(y);
    public int GetHashCode(byte[] obj) => obj != null ? obj.Aggregate(17, (h, b) => h * 31 + b) : 0;
}


public class TradeCardsTransactionSerializationTests
{
    private (string, int, object)[] MakeFormat(TradeCardsTransaction tx)
    {
        var user1Key = tx.User1PublicKey;
        var user2Key = tx.User2PublicKey;
        var cardsFromUser1 = tx.CardsFromUser1.ToArray();
        var cardsFromUser2 = tx.CardsFromUser2.ToArray();
        var sig1 = tx.User1Signature;
        var sig2 = tx.User2Signature;
        bool hasSig1 = sig1 != null && sig1.Length > 0;
        bool hasSig2 = sig2 != null && sig2.Length > 0;
        return new (string, int, object)[]
        {
            ("version", 1, (byte)0x01),
            ("discriminator", 1, (byte)0x02),
            ("user1KeyLen", 4, user1Key.Length),
            ("user1Key", user1Key.Length, user1Key),
            ("user2KeyLen", 4, user2Key.Length),
            ("user2Key", user2Key.Length, user2Key),
            ("cardsFromUser1Count", 4, cardsFromUser1.Length),
            ("cardsFromUser1", cardsFromUser1.Length * 32, cardsFromUser1.SelectMany(x => x).ToArray()),
            ("cardsFromUser2Count", 4, cardsFromUser2.Length),
            ("cardsFromUser2", cardsFromUser2.Length * 32, cardsFromUser2.SelectMany(x => x).ToArray()),
            ("timestamp", 8, BitConverter.GetBytes(tx.Timestamp.ToBinary())),
            ("user1SignatureLen", 4, hasSig1 ? sig1.Length : 0),
            ("user1Signature", hasSig1 ? sig1.Length : 0, hasSig1 ? sig1 : Array.Empty<byte>()),
            ("user2SignatureLen", 4, hasSig2 ? sig2.Length : 0),
            ("user2Signature", hasSig2 ? sig2.Length : 0, hasSig2 ? sig2 : Array.Empty<byte>())
        };
    }

    private void ValidateFormat((string, int, object)[] format, byte[] bytes)
    {
        int offset = 0;
        foreach (var (name, length, expected) in format)
        {
            var slice = bytes.Skip(offset).Take(length).ToArray();
            if (expected is byte b)
                Assert.Equal(b, slice[0]);
            else if (expected is byte[] arr)
                Assert.Equal(arr, slice);
            offset += length;
        }
        Assert.Equal(bytes.Length, offset);
    }

    [Fact]
    public void ValidateBinaryFormatForTradeCardsTransactionSerializer_NoSignatures()
    {
        // Arrange
        var user1Key = Wallets.User1.PublicKey;
        var user2Key = Wallets.User2.PublicKey;
        var cardsFromUser1 = new[] { Cards.Bulbasaur.Hash };
        var cardsFromUser2 = new[] { Cards.Charmander.Hash };
        var timestamp = DateTime.Parse("2020-01-01");
        var tx = new TradeCardsTransaction(user1Key, user2Key, cardsFromUser1, cardsFromUser2, timestamp);

        // Act
        var bytes = new TradeCardsTransactionSerializer().Serialize(tx);

        // Assert
        var format = MakeFormat(tx);
        ValidateFormat(format, bytes);
    }

    [Fact]
    public void ValidateBinaryFormatForTradeCardsTransactionSerializer_WithSignatures()
    {
        // Arrange
        var user1Key = Wallets.User1.PublicKey;
        var user2Key = Wallets.User2.PublicKey;
        var cardsFromUser1 = new[] { Cards.Bulbasaur.Hash };
        var cardsFromUser2 = new[] { Cards.Charmander.Hash };
        var timestamp = DateTime.Parse("2020-01-01");
        var sig1 = Enumerable.Range(0, 64).Select(i => (byte)i).ToArray();
        var sig2 = Enumerable.Range(100, 64).Select(i => (byte)i).ToArray();
        var tx = new TradeCardsTransaction(user1Key, user2Key, cardsFromUser1, cardsFromUser2, timestamp, sig1, sig2);

        // Act
        var bytes = new TradeCardsTransactionSerializer().Serialize(tx);

        // Assert
        var format = MakeFormat(tx);
        ValidateFormat(format, bytes);
    }

    [Fact]
    public void RoundTrip_Serialize_Deserialize_MatchesOriginalFields_NoSignatures()
    {
        // Arrange
        var user1Key = Wallets.User1.PublicKey;
        var user2Key = Wallets.User2.PublicKey;
        var cardsFromUser1 = new[] { Cards.Bulbasaur.Hash };
        var cardsFromUser2 = new[] { Cards.Charmander.Hash };
        var timestamp = DateTime.Parse("2020-01-01");
        var tx = new TradeCardsTransaction(user1Key, user2Key, cardsFromUser1, cardsFromUser2, timestamp);

        // Act
        var bytes = new TradeCardsTransactionSerializer().Serialize(tx);
        var deserialized = new TradeCardsTransactionSerializer().Deserialize(bytes);

        // Assert
        Assert.Equal(tx.User1PublicKey, deserialized.User1PublicKey);
        Assert.Equal(tx.User2PublicKey, deserialized.User2PublicKey);
        Assert.Equal(tx.CardsFromUser1, deserialized.CardsFromUser1, new ByteArrayComparer());
        Assert.Equal(tx.CardsFromUser2, deserialized.CardsFromUser2, new ByteArrayComparer());
        Assert.Equal(tx.Timestamp, deserialized.Timestamp);
        Assert.Null(deserialized.User1Signature);
        Assert.Null(deserialized.User2Signature);
    }

    [Fact]
    public void RoundTrip_Serialize_Deserialize_MatchesOriginalFields_WithSignatures()
    {
        // Arrange
        var user1Key = Wallets.User1.PublicKey;
        var user2Key = Wallets.User2.PublicKey;
        var cardsFromUser1 = new[] { Cards.Bulbasaur.Hash };
        var cardsFromUser2 = new[] { Cards.Charmander.Hash };
        var timestamp = DateTime.Parse("2020-01-01");
        var sig1 = Enumerable.Range(0, 64).Select(i => (byte)i).ToArray();
        var sig2 = Enumerable.Range(100, 64).Select(i => (byte)i).ToArray();
        var tx = new TradeCardsTransaction(user1Key, user2Key, cardsFromUser1, cardsFromUser2, timestamp, sig1, sig2);

        // Act
        var bytes = new TradeCardsTransactionSerializer().Serialize(tx);
        var deserialized = new TradeCardsTransactionSerializer().Deserialize(bytes);

        // Assert
        Assert.Equal(tx.User1PublicKey, deserialized.User1PublicKey);
        Assert.Equal(tx.User2PublicKey, deserialized.User2PublicKey);
        Assert.Equal(tx.CardsFromUser1, deserialized.CardsFromUser1, new ByteArrayComparer());
        Assert.Equal(tx.CardsFromUser2, deserialized.CardsFromUser2, new ByteArrayComparer());
        Assert.Equal(tx.Timestamp, deserialized.Timestamp);
        Assert.Equal(tx.User1Signature, deserialized.User1Signature);
        Assert.Equal(tx.User2Signature, deserialized.User2Signature);
    }
}
