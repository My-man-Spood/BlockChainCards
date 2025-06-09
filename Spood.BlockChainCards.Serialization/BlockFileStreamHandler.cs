namespace Spood.BlockChainCards.Serialization;

using Spood.BlockChainCards.Lib;

public class BlockFileStreamHandler : IDisposable
{
    private readonly string path;
    private readonly FileStream stream;
    private const int Int32Size = 4; // Size of a 32-bit integer
    
    public string FileName => Path.GetFileName(path);

    public BlockFileStreamHandler(string path, FileMode mode, FileAccess access, FileShare share = FileShare.Read)
    {
        this.path = path;
        this.stream = new FileStream(path, mode, access, share);
    }
    
    /// <summary>
    /// Reads the number of blocks stored in the file header.
    /// </summary>
    public int ReadBlockCount()
    {
        long originalPosition = stream.Position;
        stream.Position = 0;
        int blockCount = stream.ReadInt32();
        stream.Position = originalPosition;
        return blockCount;
    }
    
    /// <summary>
    /// Positions the stream after the block count header, ready to read the first block.
    /// </summary>
    public void PositionAtFirstBlock()
    {
        stream.Position = Int32Size;
    }
    
    /// <summary>
    /// Skips a specified number of blocks from the beginning of the file.
    /// </summary>
    /// <param name="blockCount">Number of blocks to skip</param>
    public void SkipBlocks(int blockCount)
    {
        if (blockCount <= 0) return;
        
        PositionAtFirstBlock();
        for (int i = 0; i < blockCount; i++)
        {
            SkipBlock();
        }
    }
    
    /// <summary>
    /// Prepares to read blocks from the specified startIndex within the file.
    /// </summary>
    /// <param name="startIndex">The local block index (0-based) to start from</param>
    /// <param name="totalBlocks">Total number of blocks in the file</param>
    public void PrepareToReadFrom(int startIndex, int totalBlocks)
    {
        if (startIndex <= 0)
        {
            PositionAtFirstBlock();
            return;
        }
        
        int blocksToSkip = Math.Min(startIndex, totalBlocks);
        SkipBlocks(blocksToSkip);
    }
    
    /// <summary>
    /// Seeks to the specified block position and reads its data.
    /// </summary>
    /// <param name="blockOffset">The offset of the block in the file</param>
    /// <returns>A BlockReadResult containing the block, its length, and the data offset</returns>
    public BlockReadResult ReadBlockAt(int blockOffset)
    {
        stream.Position = blockOffset;
        int blockLength = stream.ReadInt32();
        int dataOffset = blockOffset + Int32Size;
        
        var blockData = new byte[blockLength];
        stream.Read(blockData, 0, blockLength);
        var block = BlockSerializer.Deserialize(blockData);
        
        return new BlockReadResult(block, blockLength, dataOffset);
    }
    
    /// <summary>
    /// Skips the specified number of blocks from the current position.
    /// </summary>
    /// <returns>The offset of the next block</returns>
    public int SkipBlock()
    {
        int blockLen = stream.ReadInt32();
        stream.Position += blockLen;
        return (int)stream.Position;
    }
    
    /// <summary>
    /// Reads an int32 from the current stream position.
    /// </summary>
    public int ReadInt32() => stream.ReadInt32();
    
    /// <summary>
    /// Gets or sets the position within the stream.
    /// </summary>
    public long Position => stream.Position;

    public void Dispose()
    {
        stream.Dispose();
    }
}
