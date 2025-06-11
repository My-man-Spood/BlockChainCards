namespace Spood.BlockChainCards.Serialization;

using Spood.BlockChainCards.Lib.ByteUtils;

public class BlockFileStreamHandler : IDisposable
{
    private readonly string path;
    private readonly FileStream stream;
    private const int Int32Size = 4; // Size of a 32-bit integer

    private int? blockCount = null; // Cached block count from header
    private int blocksRead = 0; // How many blocks have been read since last reset

    public int BlockCount => blockCount ??= ReadBlockCount();
    public int BlocksRead => blocksRead;
    
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
        blocksRead = 0;
    }

    /// <summary>
    /// Seeks the stream to the specified offset.
    /// </summary>
    public void SeekToOffset(long offset)
    {
        stream.Position = offset;
        blocksRead = 0;
    }

    /// <summary>
    /// Returns true if there are more blocks to read in this file.
    /// </summary>
    public bool HasMoreBlocks()
    {
        if (blocksRead >= BlockCount)
            return false;
        // Save current position
        long originalPosition = stream.Position;
        try
        {
            // If at or past end, no more blocks
            if (stream.Position >= stream.Length)
                return false;
            // Try to read the next block length (Int32)
            if (stream.Position + Int32Size > stream.Length)
                return false;
            int blockLength = 0;
            try
            {
                blockLength = stream.ReadInt32();
            }
            catch
            {
                return false;
            }
            // If block length is invalid or would run past EOF, no more blocks
            if (blockLength <= 0 || stream.Position + blockLength > stream.Length)
                return false;
            return true;
        }
        finally
        {
            // Restore position
            stream.Position = originalPosition;
        }
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
        return ReadBlockAtCurrentPosition();
    }
    
    /// <summary>
    /// Reads a block from the current stream position.
    /// </summary>
    /// <returns>A BlockReadResult containing the block, its length, and the data offset</returns>
    public BlockReadResult ReadBlockAtCurrentPosition()
    {
        int currentPosition = (int)stream.Position;
        int blockLength = stream.ReadInt32();
        int dataOffset = currentPosition + Int32Size;
        
        var blockData = new byte[blockLength];
        stream.Read(blockData, 0, blockLength);
        var block = BlockSerializer.Deserialize(blockData);
        blocksRead++;
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
    /// Gets the position within the stream.
    /// </summary>
    public long Position => stream.Position;
    
    /// <summary>
    /// Appends a serialized block to the end of the file.
    /// </summary>
    /// <param name="serializedBlock">The serialized block data to append</param>
    /// <returns>The offset where the block data begins (after length prefix)</returns>
    public int AppendSerializedBlock(byte[] serializedBlock)
    {
        // Position at the end of the file
        stream.Position = stream.Length;
        long blockOffset = stream.Position;
        
        // Write length prefix followed by block data
        byte[] lengthBytes = serializedBlock.Length.AsBytes();
        stream.Write(lengthBytes, 0, Int32Size);
        stream.Write(serializedBlock, 0, serializedBlock.Length);
        
        // Return the offset of the actual block data (after length prefix)
        return (int)blockOffset + Int32Size;
    }
    
    /// <summary>
    /// Reads and increments the block count in the file header.
    /// </summary>
    /// <returns>The new block count after incrementing</returns>
    public int IncrementBlockCount()
    {
        // Store current position
        long originalPosition = stream.Position;
        
        // Go to beginning of file and read current count
        stream.Position = 0;
        int currentCount = stream.ReadInt32();
        
        // Increment count and write it back
        int newCount = currentCount + 1;
        stream.Position = 0;
        byte[] countBytes = newCount.AsBytes();
        stream.Write(countBytes, 0, Int32Size);
        
        // Restore original position
        stream.Position = originalPosition;
        
        return newCount;
    }

    public void Dispose()
    {
        stream.Dispose();
    }
}
