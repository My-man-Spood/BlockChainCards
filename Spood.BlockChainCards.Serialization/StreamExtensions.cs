namespace Spood.BlockChainCards.Serialization;

public static class StreamExtensions
{
    public static int ReadInt32(this Stream stream)
    {
        var buffer = new byte[4];
        stream.Read(buffer, 0, 4);
        return BitConverter.ToInt32(buffer, 0);
    }
}
