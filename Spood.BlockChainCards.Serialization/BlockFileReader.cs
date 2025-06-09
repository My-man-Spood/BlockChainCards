namespace Spood.BlockChainCards.Serialization;

using Spood.BlockChainCards.Lib;
using Spood.BlockChainCards.Lib.Utils;

public class BlockFileReader
{
    private readonly string filePath;
    public const int BlocksPerFile = 1000;
    public BlockFileReader(string filePath)
    {
        this.filePath = filePath;
    }

    public void InitializeBlockChain()
    {
        var initialBlockPath = $"_{0:D6}.blk";
        if (File.Exists(initialBlockPath))
        {
            throw new InvalidOperationException("Block file already exists");
        }

        CreateEmptyBlockFile(initialBlockPath);
    }

    public string GetOpenBlockFilePath()
    {
        var files = Directory.GetFiles(filePath, "_*.blk");
        return files[^1];
    }

    public AppendBlockResult AppendBlock(BCBlock block)
    {
        var serializedBlock = BlockSerializer.Serialize(block);
        var openBlockPath = GetOpenBlockFilePath();

        using var stream = new FileStream(openBlockPath, FileMode.Open, FileAccess.Write, FileShare.None);
        var blockOffset = stream.Seek(0, SeekOrigin.End);
        stream.Write(serializedBlock.LengthBytesLE(), 0, 4);
        stream.Write(serializedBlock, 0, serializedBlock.Length);
        int newCount = IncrementBlockCount(stream);

        RotateBlockFileIfFull(openBlockPath, newCount);
        // Add 4 to blockOffset to account for the length prefix
        var files = Directory.GetFiles(filePath, "*.blk").OrderBy(f => f).ToArray();
        int fileIdx = Array.FindIndex(files, f => Path.GetFileName(f) == Path.GetFileName(openBlockPath));
        int globalIndex = fileIdx * BlocksPerFile + (newCount - 1);
        return new AppendBlockResult(block.Hash, Path.GetFileName(openBlockPath), globalIndex, (int)blockOffset+4, serializedBlock.Length);
    }

    public int GetTotalBlockCount()
    {
        var total = 0;
        var files = Directory.GetFiles(filePath, "*.blk");
        total += (files.Length-1) * BlocksPerFile; 
        var lastFile = files[^1];
        using var stream = new FileStream(lastFile, FileMode.Open, FileAccess.Read, FileShare.Read);
        var countBuffer = new byte[4];
        stream.Read(countBuffer, 0, 4);
        total += BitConverter.ToInt32(countBuffer);

        return total;
    }

    /// <summary>
    /// Enumerates all blocks in all block files, yielding the block, its AppendBlockResult (file, local index, offset, size), and the global height.
    /// </summary>
    public IEnumerable<AppendBlockResult> EnumerateBlocksWithResults(int startHeight = 0)
    {
        var files = Directory.GetFiles(filePath, "*.blk").OrderBy(f => f).ToArray();
        int globalHeight = 0;
        for (int f = 0; f < files.Length; f++)
        {
            var file = files[f];
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            var countBuffer = new byte[4];
            stream.Read(countBuffer, 0, 4);
            int blockCount = BitConverter.ToInt32(countBuffer, 0);
            int offset = 4;
            for (int i = 0; i < blockCount; i++)
            {
                if (globalHeight >= startHeight)
                {
                    stream.Seek(offset, SeekOrigin.Begin);
                    var lenBuffer = new byte[4];
                    stream.Read(lenBuffer, 0, 4);
                    int blockLen = BitConverter.ToInt32(lenBuffer, 0);
                    var blockData = new byte[blockLen];
                    stream.Read(blockData, 0, blockLen);
                    var block = BlockSerializer.Deserialize(blockData);
                    var result = new AppendBlockResult(block.Hash, Path.GetFileName(file), globalHeight, offset + 4, blockLen);
                    yield return result;
                    offset += 4 + blockLen;
                }
                else
                {
                    stream.Seek(offset, SeekOrigin.Begin);
                    var lenBuffer = new byte[4];
                    stream.Read(lenBuffer, 0, 4);
                    int blockLen = BitConverter.ToInt32(lenBuffer, 0);
                    offset += 4 + blockLen;
                }
                globalHeight++;
            }
        }
    }



    public static BCBlock ReadBlockDirect(string blockFilePath, int blockOffset)
    {
        using var stream = new FileStream(blockFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        stream.Seek(blockOffset, SeekOrigin.Begin);
        var blockLengthBytes = new byte[4];
        stream.Read(blockLengthBytes, 0, 4);
        var blockLength = BitConverter.ToInt32(blockLengthBytes, 0);
        var blockData = new byte[blockLength];
        stream.Read(blockData, 0, blockLength);

        return BlockSerializer.Deserialize(blockData);
    }

    public static IEnumerable<BCBlock> ReadBlockFile(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var blockCountBytes = new byte[4];
        stream.Read(blockCountBytes, 0, 4);
        var blockCount = BitConverter.ToInt32(blockCountBytes, 0);
        for (int i = 0; i < blockCount; i++)
        {
            var blockLengthBytes = new byte[4];
            stream.Read(blockLengthBytes, 0, 4);
            var blockLength = BitConverter.ToInt32(blockLengthBytes, 0);
            var blockData = new byte[blockLength];
            stream.Read(blockData, 0, blockLength);
            yield return BlockSerializer.Deserialize(blockData);
        }
    }

    private static int IncrementBlockCount(FileStream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);
        var countBuffer = new byte[4];
        stream.Read(countBuffer, 0, 4);
        int count = BitConverter.ToInt32(countBuffer, 0) + 1;
        stream.Seek(0, SeekOrigin.Begin);
        stream.Write(BitConverter.GetBytes(count), 0, 4);
        stream.Flush();
        return count;
    }

    private static void RotateBlockFileIfFull(string openBlockPath, int blockCount)
    {   
        if (blockCount == BlocksPerFile)
        {
            CloseBlock(openBlockPath);
            CreateEmptyBlockFile(GetNextBlockFilePath(openBlockPath));
        }
    }

    private static void CloseBlock(string openBlockPath)
    {
        File.Move(openBlockPath, openBlockPath.Replace("_", ""));
    }

    private static string GetNextBlockFilePath(string currentBlockFilePath)
    {
        var nextNumber = int.Parse(currentBlockFilePath.Replace("_", "")) + 1;
        return $"_{nextNumber:D6}.blk";
    }
    
    private static void CreateEmptyBlockFile(string newBlockPath)
    {
        using var filestream = File.Create(newBlockPath);

        // Write initial block count (0)
        filestream.Write(BitConverter.GetBytes(0), 0, 4);
    }

}