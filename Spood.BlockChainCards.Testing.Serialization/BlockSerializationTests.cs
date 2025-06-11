using Spood.BlockChainCards.Lib;
using Spood.BlockChainCards.Lib.Transactions;
using Spood.BlockChainCards.Serialization;
using Spood.BlockChainCards.Serialization.Transactions;
using Spood.BlockChainCards.Testing.Lib.TestApi;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Spood.BlockChainCards.Testing.Serialization;

public class BlockSerializationTests
{
    private (string, int, object)[] MakeFormat(BCBlock block, List<byte[]> txBytes)
    {
        return new (string, int, object)[]
        {
            ("version", 1, (byte)0x01),
            ("prevHash", 32, block.PreviousHash),
            ("timestamp", 8, BitConverter.GetBytes(block.Timestamp.ToBinary())),
            ("txCount", 4, BitConverter.GetBytes(txBytes.Count)),
            // Transaction sections checked separately
        };
    }

    private void ValidateFormat((string, int, object)[] format, byte[] bytes, List<byte[]> transactions)
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
        // Now check transactions
        foreach (var tx in transactions)
        {
            var txLen = BitConverter.ToInt32(bytes, offset);
            Assert.Equal(tx.Length, txLen);
            offset += 4;
            Assert.Equal(tx, bytes.Skip(offset).Take(txLen).ToArray());
            offset += txLen;
        }
        Assert.Equal(bytes.Length, offset);
    }

    [Fact]
    public void ValidateBinaryFormat_BlockWithNoTransactions()
    {
        var prevHash = Enumerable.Repeat((byte)0xAB, 32).ToArray();
        var block = new BCBlock()
        {
            PreviousHash = prevHash,
            Transactions = new List<BCTransaction>(),
            Timestamp = DateTime.Parse("2023-01-01T00:00:00Z"),
        };
        var txBytes = new List<byte[]>();
        var bytes = BlockSerializer.Serialize(block);
        var format = MakeFormat(block, txBytes);
        ValidateFormat(format, bytes, txBytes);
    }

    [Fact]
    public void ValidateBinaryFormat_BlockWithMintTransaction()
    {
        var prevHash = Enumerable.Repeat((byte)0xCD, 32).ToArray();
        var mintTx = new MintCardTransaction(Wallets.Authority.PublicKey, Wallets.User1.PublicKey, Cards.Bulbasaur.Hash, DateTime.Parse("2023-02-01T00:00:00Z"));
        mintTx.Sign(Wallets.Authority.PrivateKey);
        var block = new BCBlock()
        {
            PreviousHash = prevHash,
            Transactions = new List<BCTransaction> { mintTx },
            Timestamp = DateTime.Parse("2023-02-02T00:00:00Z"),
        };
        var txBytes = new List<byte[]> { new MintCardTransactionSerializer().Serialize(mintTx) };
        var bytes = BlockSerializer.Serialize(block);
        var format = MakeFormat(block, txBytes);
        ValidateFormat(format, bytes, txBytes);
    }

    [Fact]
    public void ValidateBinaryFormat_BlockWithTradeTransaction()
    {
        var prevHash = Enumerable.Repeat((byte)0xEF, 32).ToArray();
        var tradeTx = new TradeCardsTransaction(Wallets.User1.PublicKey, Wallets.User2.PublicKey, new[] { Cards.Bulbasaur.Hash }, new[] { Cards.Charmander.Hash }, DateTime.Parse("2023-03-01T00:00:00Z"));
        tradeTx.Sign(Wallets.User1.PrivateKey);
        tradeTx.Sign(Wallets.User2.PrivateKey);
        var block = new BCBlock()
        {
            PreviousHash = prevHash,
            Transactions = new List<BCTransaction> { tradeTx },
            Timestamp = DateTime.Parse("2023-03-02T00:00:00Z"),
        };
        var txBytes = new List<byte[]> { new TradeCardsTransactionSerializer().Serialize(tradeTx) };
        var bytes = BlockSerializer.Serialize(block);
        var format = MakeFormat(block, txBytes);
        ValidateFormat(format, bytes, txBytes);
    }

    [Fact]
    public void RoundTrip_Serialize_Deserialize_BlockWithMultipleTransactions()
    {
        var prevHash = Enumerable.Repeat((byte)0xAA, 32).ToArray();
        var mintTx = new MintCardTransaction(Wallets.Authority.PublicKey, Wallets.User1.PublicKey, Cards.Bulbasaur.Hash, DateTime.Parse("2023-04-01T00:00:00Z"));
        mintTx.Sign(Wallets.Authority.PrivateKey);
        var tradeTx = new TradeCardsTransaction(Wallets.User1.PublicKey, Wallets.User2.PublicKey, new[] { Cards.Bulbasaur.Hash }, new[] { Cards.Charmander.Hash }, DateTime.Parse("2023-04-02T00:00:00Z"));
        tradeTx.Sign(Wallets.User1.PrivateKey);
        tradeTx.Sign(Wallets.User2.PrivateKey);
        var block = new BCBlock()
        {
            PreviousHash = prevHash,
            Transactions = new List<BCTransaction> { mintTx, tradeTx },
            Timestamp = DateTime.Parse("2023-04-03T00:00:00Z"),
        };
        var bytes = BlockSerializer.Serialize(block);
        var deserialized = BlockSerializer.Deserialize(bytes);
        Assert.Equal(block.PreviousHash, deserialized.PreviousHash);
        Assert.Equal(block.Timestamp, deserialized.Timestamp);
        Assert.Equal(block.Transactions.Count, deserialized.Transactions.Count);
        // Transaction round-trip
        for (int i = 0; i < block.Transactions.Count; i++)
        {
            Assert.Equal(block.Transactions[i].GetType(), deserialized.Transactions[i].GetType());
            Assert.Equal(block.Transactions[i].Timestamp, deserialized.Transactions[i].Timestamp);
            Assert.Equal(block.Transactions[i].GetAllCards(), deserialized.Transactions[i].GetAllCards(), new ByteArrayComparer());
        }
    }
}
