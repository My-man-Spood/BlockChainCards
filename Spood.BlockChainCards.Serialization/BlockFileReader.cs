namespace Spood.BlockChainCards.Serialization;

using Spood.BlockChainCards.Lib;
public class BlockFileReader
{
    private readonly string filePath;

    public BlockFileReader(string filePath)
    {
        this.filePath = filePath;
    }

    public BCBlock ReadBlock(int blockIndex)
    {
    }

    public int GetLastBlockIndex()
    {
    }

    public void AppendBlock(byte[] blockData, string filePath)
    {
        int blockLength = blockData.Length;
        using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.None))
        {
            // Write the block length as a 4-byte integer
            byte[] lengthBytes = BitConverter.GetBytes(blockLength);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(lengthBytes);
            }
            stream.Seek(0, SeekOrigin.End); // Move to the end of the file
            stream.Write(lengthBytes, 0, lengthBytes.Length);
            // Write the block data
            stream.Write(blockData, 0, blockData.Length);

            // Go back to start of the file and increment block count
            stream.Seek(0, SeekOrigin.Begin);
            var blockCountBuffer = new byte[4];
            stream.Read(blockCountBuffer, 0, blockCountBuffer.Length);
            int blockCount = BitConverter.ToInt32(blockCountBuffer, 0);
            blockCount++;
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(blockCountBuffer);
            }
            stream.Seek(0, SeekOrigin.Begin); // Move to the start of the file
            stream.Write(BitConverter.GetBytes(blockCount), 0, 4); // Write the updated block count
            stream.Flush();
        }
    }
}