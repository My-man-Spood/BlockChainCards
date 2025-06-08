using Spood.BlockChainCards.Lib.Transactions;
using Spood.BlockChainCards.Serialization.Transactions;
using Spood.BlockChainCards.Testing.Lib.TestApi;

namespace Spood.BlockChainCards.Testing.Serialization;

public class MintCardTransactionSerializationTests
{
    private (string, int, object)[] MakeFormat(MintCardTransaction tx)
    {
        // (name, length, expectedValue)
        return new (string, int, object)[]
        {
            ("version", 1, (byte)0x01), // static value
            ("discriminator", 1, (byte)0x01), // static for MintCardTransaction
            ("authorityKeyLen", 4, tx.AuthorityPublicKey.Length),
            ("authoritypublickey", tx.AuthorityPublicKey.Length, tx.AuthorityPublicKey),
            ("recipientKeyLen", 4, tx.RecipientPublicKey.Length),
            ("recipientpublickey", tx.RecipientPublicKey.Length, tx.RecipientPublicKey),
            ("card", 32, tx.Card),
            ("timestamp", 8, BitConverter.GetBytes(tx.Timestamp.ToBinary())),
            ("authoritySignatureLen", 4, tx.AuthoritySignature?.Length ?? 0),
            ("authoritySignature", tx.AuthoritySignature?.Length ?? 0, tx.AuthoritySignature)
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
        Assert.Equal(bytes.Length, offset); // No extra bytes
    }

    [Fact]
    public void ValidateBinaryFormatForMintTransactionSerializer_NoSignature()
    {
        // Arrange
        var authorityKey = Wallets.Authority.PublicKey;
        var recipientKey = Wallets.User1.PublicKey;
        var card = Cards.Charmander.Hash;
        var timestamp = DateTime.Parse("2020-01-01");
        var tx = new MintCardTransaction(authorityKey, recipientKey, card, timestamp);

        // Act
        var bytes = TransactionSerializer.Serialize(tx);

        // Assert
        var format = MakeFormat(tx);
        ValidateFormat(format, bytes);
    }

    [Fact]
    public void ValidateBinaryFormatForMintTransactionSerializer_WithSignature()
    {
        // Arrange
        var authorityKey = Wallets.Authority.PublicKey;
        var recipientKey = Wallets.User1.PublicKey;
        var card = Cards.Charmander.Hash;
        var timestamp = DateTime.Parse("2020-01-01");
        var signature = Enumerable.Range(0, 64).Select(i => (byte)i).ToArray(); // dummy signature
        var tx = new MintCardTransaction(authorityKey, recipientKey, card, timestamp, signature);

        // Act
        var bytes = TransactionSerializer.Serialize(tx);

        // Assert
        var format = MakeFormat(tx);
        ValidateFormat(format, bytes);
    }

    [Fact]
    public void RoundTrip_Serialize_Deserialize_MatchesOriginalFields_NoSignature()
    {
        // Arrange
        var authorityKey = Wallets.Authority.PublicKey;
        var recipientKey = Wallets.User1.PublicKey;
        var card = Cards.Charmander.Hash;
        var timestamp = DateTime.Parse("2020-01-01");
        var tx = new MintCardTransaction(authorityKey, recipientKey, card, timestamp);

        // Act
        var bytes = TransactionSerializer.Serialize(tx);
        var deserialized = TransactionSerializer.Deserialize<MintCardTransaction>(bytes);

        // Assert
        Assert.Equal(tx.AuthorityPublicKey, deserialized.AuthorityPublicKey);
        Assert.Equal(tx.RecipientPublicKey, deserialized.RecipientPublicKey);
        Assert.Equal(tx.Card, deserialized.Card);
        Assert.Equal(tx.Timestamp, deserialized.Timestamp);
    }

    [Fact]
    public void RoundTrip_Serialize_Deserialize_MatchesOriginalFields_WithSignature()
    {
        // Arrange
        var authorityKey = Wallets.Authority.PublicKey;
        var recipientKey = Wallets.User1.PublicKey;
        var card = Cards.Charmander.Hash;
        var timestamp = DateTime.Parse("2020-01-01");
        var signature = Enumerable.Range(0, 64).Select(i => (byte)i).ToArray(); // dummy signature
        var tx = new MintCardTransaction(authorityKey, recipientKey, card, timestamp, signature);

        // Act
        var bytes = TransactionSerializer.Serialize(tx);
        var deserialized = TransactionSerializer.Deserialize<MintCardTransaction>(bytes);

        // Assert
        Assert.Equal(tx.AuthorityPublicKey, deserialized.AuthorityPublicKey);
        Assert.Equal(tx.RecipientPublicKey, deserialized.RecipientPublicKey);
        Assert.Equal(tx.Card, deserialized.Card);
        Assert.Equal(tx.Timestamp, deserialized.Timestamp);
        Assert.Equal(tx.AuthoritySignature, deserialized.AuthoritySignature);
    }
}

