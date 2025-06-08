using Spood.BlockChainCards.Lib.Transactions;

namespace Spood.BlockChainCards.Serialization.Transactions;

public class MintCardTransactionSerializer
{
    private const byte modelVersion = 1;
    public const byte discriminator = 1;
    public MintCardTransaction Transaction;

    public byte[] Serialize(MintCardTransaction transaction)
    {
        this.Transaction = transaction;

        // Calculate lengths
        int totalLength =
            1 + // modelVersion
            1 + // discriminator
            4 + // authority public key length
            transaction.AuthorityPublicKey.Length + // authority public key bytes
            4 + // recipient public key length
            transaction.RecipientPublicKey.Length + // recipient public key bytes
            32 + // card hash
            8 +  // timestamp
            4 + (transaction.AuthoritySignature == null ? 0 : transaction.AuthoritySignature.Length); // signatureLen + signature bytes (if present)

        byte[] buffer = new byte[totalLength];
        int offset = 0;

        buffer[offset++] = modelVersion;
        buffer[offset++] = discriminator;
        offset = WriteAuthorityPubKeyToBuffer(transaction.AuthorityPublicKey, buffer, offset);
        offset = WriteRecipientPubKeyToBuffer(transaction.RecipientPublicKey, buffer, offset);
        offset = WriteCardToBuffer(transaction.Card, buffer, offset);
        offset = WriteTimestampToBuffer(transaction.Timestamp, buffer, offset);
        offset = WriteSignatureToBuffer(transaction.AuthoritySignature, buffer, offset);

        return buffer;
    }

    private static int WriteAuthorityPubKeyToBuffer(byte[] authorityKey, byte[] buffer, int offset)
    {
        SerializationUtils.WriteInt32LittleEndianToArray(buffer, offset, authorityKey.Length);
        offset += 4;
        Array.Copy(authorityKey, 0, buffer, offset, authorityKey.Length);
        
        return offset + authorityKey.Length;
    }

    private static int WriteRecipientPubKeyToBuffer(byte[] recipientKey, byte[] buffer, int offset)
    {
        SerializationUtils.WriteInt32LittleEndianToArray(buffer, offset, recipientKey.Length);
        offset += 4;
        Array.Copy(recipientKey, 0, buffer, offset, recipientKey.Length);
        
        return offset + recipientKey.Length;
    }

    private static int WriteCardToBuffer(byte[] card, byte[] buffer, int offset)
    {
        Array.Copy(card, 0, buffer, offset, 32);
        return offset + 32;
    }

    private static int WriteTimestampToBuffer(DateTime timestamp, byte[] buffer, int offset)
    {
        SerializationUtils.WriteInt64LittleEndianToArray(buffer, offset, timestamp.ToBinary());
        return offset + 8;
    }

    private static int WriteSignatureToBuffer(byte[] signature, byte[] buffer, int offset)
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

    public MintCardTransaction Deserialize(byte[] bytes)
    {
        int offset = 0;
        // Version
        byte version = bytes[offset++];
        // Discriminator
        byte disc = bytes[offset++];
        if (disc != discriminator)
            throw new ArgumentException($"Discriminator mismatch: expected {discriminator}, got {disc}");

        var authorityKey = ReadAuthorityPubKeyFromBuffer(bytes, ref offset);
        var recipientKey = ReadRecipientPubKeyFromBuffer(bytes, ref offset);
        var card = ReadCardFromBuffer(bytes, ref offset);
        var timestamp = ReadTimestampFromBuffer(bytes, ref offset);
        var signature = ReadSignatureFromBuffer(bytes, ref offset);

        var tx = new MintCardTransaction(authorityKey, recipientKey, card, timestamp, signature);
        return tx;
    }

    private static byte[] ReadAuthorityPubKeyFromBuffer(byte[] buffer, ref int offset)
    {
        int len = SerializationUtils.ReadInt32LittleEndianFromArray(buffer, offset);
        offset += 4;
        var key = new byte[len];
        Array.Copy(buffer, offset, key, 0, len);
        offset += len;
        return key;
    }

    private static byte[] ReadRecipientPubKeyFromBuffer(byte[] buffer, ref int offset)
    {
        int len = SerializationUtils.ReadInt32LittleEndianFromArray(buffer, offset);
        offset += 4;
        var key = new byte[len];
        Array.Copy(buffer, offset, key, 0, len);
        offset += len;
        return key;
    }

    private static byte[] ReadCardFromBuffer(byte[] buffer, ref int offset)
    {
        var card = new byte[32];
        Array.Copy(buffer, offset, card, 0, 32);
        offset += 32;
        return card;
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
