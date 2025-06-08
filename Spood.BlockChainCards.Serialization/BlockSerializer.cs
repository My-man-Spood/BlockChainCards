using Spood.BlockChainCards.Lib;
using Spood.BlockChainCards.Lib.Transactions;
using Spood.BlockChainCards.Serialization.Transactions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Spood.BlockChainCards.Serialization;

public class BlockSerializer
{
    private const byte modelVersion = 1;

    public byte[] Serialize(BCBlock block)
    {
        // Prepare
        var prevHash = block.PreviousHash;
        var transactions = block.Transactions ?? new List<BCTransaction>();
        var timestamp = block.Timestamp;

        // Serialize transactions
        var txBytesList = transactions.Select(TransactionSerializer.Serialize).ToList();
        int txCount = txBytesList.Count;
        int txSectionLen = txBytesList.Sum(b => 4 + b.Length); // 4 bytes for each tx length prefix

        int totalLength =
            1 + // version
            32 + // previous hash
            8 + // timestamp
            4 + // transaction count
            txSectionLen;

        byte[] buffer = new byte[totalLength];
        int offset = 0;
        buffer[offset++] = modelVersion;
        Array.Copy(prevHash, 0, buffer, offset, 32);
        offset += 32;
        SerializationUtils.WriteInt64LittleEndianToArray(buffer, offset, timestamp.ToBinary());
        offset += 8;
        SerializationUtils.WriteInt32LittleEndianToArray(buffer, offset, txCount);
        offset += 4;
        foreach (var txBytes in txBytesList)
        {
            SerializationUtils.WriteInt32LittleEndianToArray(buffer, offset, txBytes.Length);
            offset += 4;
            Array.Copy(txBytes, 0, buffer, offset, txBytes.Length);
            offset += txBytes.Length;
        }
        return buffer;
    }

    public BCBlock Deserialize(byte[] bytes)
    {
        int offset = 0;
        byte version = bytes[offset++];
        var prevHash = new byte[32];
        Array.Copy(bytes, offset, prevHash, 0, 32);
        offset += 32;
        long timestampBinary = SerializationUtils.ReadInt64LittleEndianFromArray(bytes, offset);
        offset += 8;
        int txCount = SerializationUtils.ReadInt32LittleEndianFromArray(bytes, offset);
        offset += 4;
        var transactions = new List<BCTransaction>(txCount);
        for (int i = 0; i < txCount; i++)
        {
            int txLen = SerializationUtils.ReadInt32LittleEndianFromArray(bytes, offset);
            offset += 4;
            var txBytes = new byte[txLen];
            Array.Copy(bytes, offset, txBytes, 0, txLen);
            offset += txLen;
            var tx = TransactionSerializer.Deserialize(txBytes);
            transactions.Add(tx);
        }
        var block = new BCBlock()
        {
            PreviousHash = prevHash,
            Transactions = transactions,
            Timestamp = DateTime.FromBinary(timestampBinary),
        };
        return block;
    }
}
