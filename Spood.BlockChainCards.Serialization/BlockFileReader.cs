namespace Spood.BlockChainCards.Serialization;

using Spood.BlockChainCards.Lib;
using Spood.BlockChainCards.Lib.ByteUtils;
using System.IO;

public class BlockFileReader
{
    private readonly string filePath;
    public const int BlocksPerFile = 1000;
    public BlockFileReader(string filePath)
    {
        this.filePath = filePath;
    }

    public void Initialize()
    {
        // Ensure the blockchain folder exists
        if (!Directory.Exists(filePath))
            Directory.CreateDirectory(filePath);

        // Ensure at least one block file exists (genesis file)
        var blockFiles = Directory.GetFiles(filePath, "*.blk");
        if (blockFiles.Length == 0)
        {
            var genesisFile = Path.Combine(filePath, "_000000.blk");
            if (!File.Exists(genesisFile))
            {
                using var filestream = File.Create(genesisFile);
                filestream.Write(BitConverter.GetBytes(0), 0, 4); // initial block count 0
            }
        }
    }

    public void InitializeBlockChain()
    {
        var initialBlockPath = Path.Combine(filePath, $"_{0:D6}.blk");
        if (File.Exists(initialBlockPath))
        {
            return;
        }

        CreateEmptyBlockFile(initialBlockPath);
    }

    public string GetOpenBlockFilePath()
    {
        var files = Directory.GetFiles(filePath, "_*.blk");
        return files[^1];
    }

    public BlockMetaData AppendBlock(BCBlock block)
    {
        // Serialize the block
        var serializedBlock = BlockSerializer.Serialize(block);
        var openBlockPath = GetOpenBlockFilePath();

        // Use the stream handler for appending
        using var stream = new BlockFileStreamHandler(openBlockPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        
        // Append the serialized block and get its offset
        int blockDataOffset = stream.AppendSerializedBlock(serializedBlock);
        
        // Increment and get the new block count
        int newCount = stream.IncrementBlockCount();

        // Rotate block file if needed
        RotateBlockFileIfFull(openBlockPath, newCount);
        
        // Calculate the global block index
        var files = Directory.GetFiles(filePath, "*.blk").OrderBy(f => f).ToArray();
        int fileIdx = Array.FindIndex(files, f => Path.GetFileName(f) == Path.GetFileName(openBlockPath));
        int globalIndex = fileIdx * BlocksPerFile + (newCount - 1);
        
        // Return metadata about the appended block
        return new BlockMetaData(
            block.Hash, 
            Path.GetFileName(openBlockPath), 
            globalIndex, 
            blockDataOffset, 
            serializedBlock.Length);
    }

    public int GetTotalBlockCount()
    {
        var total = 0;
        var files = Directory.GetFiles(filePath, "*.blk");
        if (files.Length == 0)
            return 0;
        total += (files.Length - 1) * BlocksPerFile;
        var lastFile = files[^1];
        using var stream = new FileStream(lastFile, FileMode.Open, FileAccess.Read, FileShare.Read);
        var countBuffer = new byte[4];
        stream.Read(countBuffer, 0, 4);
        total += countBuffer.ToInt32();

        return total;
    }

    /// <summary>
    /// Enumerates all blocks in all block files, yielding the block, its AppendBlockResult (file, local index, offset, size), and the global height.
    /// </summary>
    // Size of a 32-bit integer in bytes (used for length prefixes and block counts)
    private const int Int32Size = 4;

    /// <summary>
    /// Reads a block's length-prefixed data from the stream at the current position.
    /// </summary>
    private (int Length, byte[] Data) ReadBlockData(Stream stream)
    {
        int blockLen = stream.ReadInt32();
        var blockData = new byte[blockLen];
        stream.Read(blockData, 0, blockLen);
        return (blockLen, blockData);
    }

    /// <summary>
    /// Enumerates blocks starting from a global height, with optimized file and block skipping.
    /// </summary>
    /// <param name="startHeight">The global block height to start from</param>
    public IEnumerable<BlockMetaData> EnumerateBlocksMetaData(int startHeight = 0)
    {
        var files = Directory.GetFiles(filePath, "*.blk").OrderBy(f => f).ToArray();
        if (files.Length == 0)
            yield break;
            
        // Calculate which file to start from and which block within that file
        int startFileIndex = startHeight / BlocksPerFile;
        int startBlockIndex = startHeight % BlocksPerFile;
        int globalHeight = startFileIndex * BlocksPerFile;
        
        // Process each file from the starting file onwards
        for (int fileIndex = startFileIndex; fileIndex < files.Length; fileIndex++)
        {
            var file = files[fileIndex];
            using var stream = new BlockFileStreamHandler(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            
            int blockCount = stream.ReadBlockCount();
            if (blockCount == 0) continue;
            
            int blockIdxToStartFrom = (fileIndex == startFileIndex) ? startBlockIndex : 0;
            
            stream.PrepareToReadFrom(blockIdxToStartFrom, blockCount);
            
            globalHeight += blockIdxToStartFrom;
            
            // Process the remaining blocks in this file
            for (int blockIdx = blockIdxToStartFrom; blockIdx < blockCount; blockIdx++)
            {
                // Read block at current position
                BlockReadResult readResult = stream.ReadBlockAtCurrentPosition();
                
                // Create and yield result
                var result = new BlockMetaData(
                    readResult.Block.Hash, 
                    stream.FileName, 
                    globalHeight, 
                    readResult.DataOffset, 
                    readResult.Length);
                yield return result;
                
                // Increment global height for next block
                globalHeight++;
            }
        }
    }

    /// <summary>
    /// Reads a block directly from a file using the provided metadata
    /// </summary>
    /// <param name="blockFilePath">The relative path to the block file (will be combined with base path)</param>
    /// <param name="blockOffset">Offset position in the file</param>
    /// <param name="size">Size of the block in bytes</param>
    /// <returns>The deserialized block</returns>
    public BCBlock ReadBlockDirect(string blockFilePath, int blockOffset, int size)
    {
        using var stream = new FileStream(Path.Combine(filePath, blockFilePath), FileMode.Open, FileAccess.Read, FileShare.Read);
        stream.Seek(blockOffset, SeekOrigin.Begin);
        var blockData = new byte[size];
        stream.Read(blockData, 0, size);
        return BlockSerializer.Deserialize(blockData);
    }
    
    /// <summary>
    /// Streams all blocks starting from a specific BlockIndexMetaData point up to the end of the blockchain
    /// </summary>
    /// <param name="startMetaData">The metadata of the starting block</param>
    /// <returns>IEnumerable of all blocks from the starting point to the end</returns>
    public IEnumerable<BCBlock> ReadBlocksFromPoint(BlockIndexMetaData startMetaData)
    {
        // Open the starting file directly using the metadata
        using (var handler = new BlockFileStreamHandler(Path.Combine(filePath, startMetaData.FilePath), FileMode.Open, FileAccess.Read))
        {
            // Seek to the starting block's offset
            handler.SeekToOffset(startMetaData.Offset);
            // Read the starting block
            yield return handler.ReadBlockAtCurrentPosition().Block;
            // Read remaining blocks in this file
            while (handler.HasMoreBlocks())
            {
                yield return handler.ReadBlockAtCurrentPosition().Block;
            }
        }
        // Now move to subsequent files (if any)
        foreach (var file in GetOrderedBlockFilesAfter(startMetaData.FilePath))
        {
            using (var handler = new BlockFileStreamHandler(file, FileMode.Open, FileAccess.Read))
            {
                handler.PositionAtFirstBlock();
                while (handler.HasMoreBlocks())
                {
                    yield return handler.ReadBlockAtCurrentPosition().Block;
                }
            }
        }
    }

    /// <summary>
    /// Returns all block files ordered after the given file (exclusive)
    /// </summary>
    private IEnumerable<string> GetOrderedBlockFilesAfter(string filePath)
    {
        var allFiles = Directory.GetFiles(this.filePath, "_*.blk").OrderBy(f => f).ToList();
        string fileName = Path.GetFileName(filePath);
        bool found = false;
        foreach (var file in allFiles)
        {
            if (found)
                yield return file;
            if (Path.GetFileName(file).Equals(fileName, StringComparison.OrdinalIgnoreCase))
                found = true;
        }
    }
    
    private static void RotateBlockFileIfFull(string openBlockPath, int blockCount)
    {   
        if (blockCount == BlocksPerFile)
        {
            CloseBlock(openBlockPath);
            
            // Get the directory path and get next file name
            string directory = Path.GetDirectoryName(openBlockPath) ?? "";
            string nextFileName = GetNextBlockFilePath(openBlockPath);
            string nextFilePath = Path.Combine(directory, nextFileName);
            
            CreateEmptyBlockFile(nextFilePath);
        }
    }

    private static void CloseBlock(string openBlockPath)
    {
        // Create the new path in the same directory as the original file
        string directory = Path.GetDirectoryName(openBlockPath) ?? "";
        string fileName = Path.GetFileName(openBlockPath);
        string newFileName = fileName.Replace("_", "");
        string newPath = Path.Combine(directory, newFileName);
        
        File.Move(openBlockPath, newPath);
    }

    private static string GetNextBlockFilePath(string currentBlockFilePath)
    {
        // Extract just the filename
        string fileName = Path.GetFileName(currentBlockFilePath);
        
        // Remove the prefix and get the number
        string numberPart = fileName.Replace("_", "").Split('.')[0];
        int nextNumber = int.Parse(numberPart) + 1;
        
        // Create the new filename only (not full path)
        return $"_{nextNumber:D6}.blk";
    }
    
    private static void CreateEmptyBlockFile(string newBlockPath)
    {
        using var filestream = File.Create(newBlockPath);

        // Write initial block count (0)
        filestream.Write(BitConverter.GetBytes(0), 0, 4);
    }
}