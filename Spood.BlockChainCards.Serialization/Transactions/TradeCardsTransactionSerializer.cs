using Spood.BlockChainCards.Lib.Transactions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Spood.BlockChainCards.Serialization.Transactions;

public class TradeCardsTransactionSerializer
{
    private const byte modelVersion = 1;
    public const byte discriminator = 2;
    public TradeCardsTransaction Transaction;

    public byte[] Serialize(TradeCardsTransaction transaction)
    {
        this.Transaction = transaction;
        var cardsFromUser1 = transaction.CardsFromUser1.ToArray();
        var cardsFromUser2 = transaction.CardsFromUser2.ToArray();
        var sig1 = transaction.User1Signature;
        var sig2 = transaction.User2Signature;
        bool hasSig1 = sig1 != null && sig1.Length > 0;
        bool hasSig2 = sig2 != null && sig2.Length > 0;

        int totalLength =
            1 + // modelVersion
            1 + // discriminator
            4 + transaction.User1PublicKey.Length +
            4 + transaction.User2PublicKey.Length +
            4 + cardsFromUser1.Length * 32 + // cardsFromUser1 count + cards
            4 + cardsFromUser2.Length * 32 + // cardsFromUser2 count + cards
            8 + // timestamp
            4 + (hasSig1 ? sig1.Length : 0) + // user1 signature
            4 + (hasSig2 ? sig2.Length : 0);  // user2 signature

        byte[] buffer = new byte[totalLength];
        int offset = 0;
        buffer[offset++] = modelVersion;
        buffer[offset++] = discriminator;
        offset = WritePubKeyToBuffer(transaction.User1PublicKey, buffer, offset);
        offset = WritePubKeyToBuffer(transaction.User2PublicKey, buffer, offset);
        offset = WriteCardsArrayToBuffer(cardsFromUser1, buffer, offset);
        offset = WriteCardsArrayToBuffer(cardsFromUser2, buffer, offset);
        offset = WriteTimestampToBuffer(transaction.Timestamp, buffer, offset);
        offset = WriteSignatureToBuffer(sig1, buffer, offset);
        offset = WriteSignatureToBuffer(sig2, buffer, offset);
        return buffer;
    }

    public TradeCardsTransaction Deserialize(byte[] bytes)
    {
        int offset = 0;
        byte version = bytes[offset++];
        byte disc = bytes[offset++];
        if (disc != discriminator)
            throw new ArgumentException($"Discriminator mismatch: expected {discriminator}, got {disc}");
        var user1Key = ReadPubKeyFromBuffer(bytes, ref offset);
        var user2Key = ReadPubKeyFromBuffer(bytes, ref offset);
        var cardsFromUser1 = ReadCardsArrayFromBuffer(bytes, ref offset);
        var cardsFromUser2 = ReadCardsArrayFromBuffer(bytes, ref offset);
        var timestamp = ReadTimestampFromBuffer(bytes, ref offset);
        var sig1 = ReadSignatureFromBuffer(bytes, ref offset);
        var sig2 = ReadSignatureFromBuffer(bytes, ref offset);
        var tx = new TradeCardsTransaction(user1Key, user2Key, cardsFromUser1, cardsFromUser2, timestamp, sig1, sig2);
        return tx;
    }

    private static int WritePubKeyToBuffer(byte[] pubKey, byte[] buffer, int offset)
    {
        SerializationUtils.WriteInt32LittleEndianToArray(buffer, offset, pubKey.Length);
        offset += 4;
        Array.Copy(pubKey, 0, buffer, offset, pubKey.Length);
        offset += pubKey.Length;
        return offset;
    }

    private static int WriteCardsArrayToBuffer(byte[][] cards, byte[] buffer, int offset)
    {
        SerializationUtils.WriteInt32LittleEndianToArray(buffer, offset, cards.Length);
        offset += 4;
        foreach (var card in cards)
        {
            if (card.Length != 32)
                throw new ArgumentException("Card hash must be 32 bytes");
            Array.Copy(card, 0, buffer, offset, 32);
            offset += 32;
        }
        return offset;
    }

    private static int WriteTimestampToBuffer(DateTime timestamp, byte[] buffer, int offset)
    {
        SerializationUtils.WriteInt64LittleEndianToArray(buffer, offset, timestamp.ToBinary());
        return offset + 8;
    }

    private static int WriteSignatureToBuffer(byte[]? signature, byte[] buffer, int offset)
    {
        int length = (signature == null) ? 0 : signature.Length;
        SerializationUtils.WriteInt32LittleEndianToArray(buffer, offset, length);
        offset += 4;
        if (length > 0)
        {
            Array.Copy(signature, 0, buffer, offset, length);
            offset += length;
        }
        return offset;
    }

    private static byte[] ReadPubKeyFromBuffer(byte[] buffer, ref int offset)
    {
        int len = SerializationUtils.ReadInt32LittleEndianFromArray(buffer, offset);
        offset += 4;
        var key = new byte[len];
        Array.Copy(buffer, offset, key, 0, len);
        offset += len;
        return key;
    }

    private static byte[][] ReadCardsArrayFromBuffer(byte[] buffer, ref int offset)
    {
        int count = SerializationUtils.ReadInt32LittleEndianFromArray(buffer, offset);
        offset += 4;
        var cards = new byte[count][];
        for (int i = 0; i < count; i++)
        {
            var card = new byte[32];
            Array.Copy(buffer, offset, card, 0, 32);
            offset += 32;
            cards[i] = card;
        }
        return cards;
    }

    private static DateTime ReadTimestampFromBuffer(byte[] buffer, ref int offset)
    {
        long timestampBinary = SerializationUtils.ReadInt64LittleEndianFromArray(buffer, offset);
        offset += 8;
        return DateTime.FromBinary(timestampBinary);
    }

    private static byte[]? ReadSignatureFromBuffer(byte[] buffer, ref int offset)
    {
        int sigLen = SerializationUtils.ReadInt32LittleEndianFromArray(buffer, offset);
        offset += 4;
        if (sigLen > 0)
        {
            var sig = new byte[sigLen];
            Array.Copy(buffer, offset, sig, 0, sigLen);
            offset += sigLen;
            return sig;
        }
        return null;
    }
}
