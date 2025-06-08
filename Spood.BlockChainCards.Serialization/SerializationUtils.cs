namespace Spood.BlockChainCards.Serialization;

public static class SerializationUtils
{
    public static void WriteInt32LittleEndianToArray(byte[] buffer, int offset, int value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (!BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        Array.Copy(bytes, 0, buffer, offset, 4);
    }

    public static void WriteInt64LittleEndianToArray(byte[] buffer, int offset, long value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (!BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        Array.Copy(bytes, 0, buffer, offset, 8);
    }

    public static int ReadInt32LittleEndianFromArray(byte[] bytes, int offset)
    {
        var intBytes = new byte[4];
        Array.Copy(bytes, offset, intBytes, 0, 4);
        if (!BitConverter.IsLittleEndian)
            Array.Reverse(intBytes);
        return BitConverter.ToInt32(intBytes, 0);
    }

    public static long ReadInt64LittleEndianFromArray(byte[] bytes, int offset)
    {
        var longBytes = new byte[8];
        Array.Copy(bytes, offset, longBytes, 0, 8);
        if (!BitConverter.IsLittleEndian)
            Array.Reverse(longBytes);
        return BitConverter.ToInt64(longBytes, 0);
    }
}
