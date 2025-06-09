namespace Spood.BlockChainCards.Serialization;

using System.Security.Principal;
using Spood.BlockChainCards.Lib;
using Spood.BlockChainCards.Lib.Utils;

public class BlockFileReader
{
    private readonly string filePath;

    public BlockFileReader(string filePath)
    {
        this.filePath = filePath;
    }

    public string GetOpenBlockFilePath()
    {
        var files = Directory.GetFiles(filePath, "_*.blk");
        return files[^1];
    }

    public void AppendBlock(byte[] blockData)
    {
        var openBlockPath = GetOpenBlockFilePath();
        int blockCount = AppendBlockToFile(openBlockPath, blockData);
        RotateBlockFileIfFull(openBlockPath, blockCount);
    }

    private static int AppendBlockToFile(string filePath, byte[] blockData)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.None);
        WriteBlockToEnd(stream, blockData);
        int newCount = IncrementBlockCount(stream);
        return newCount;
    }

    private static void WriteBlockToEnd(FileStream stream, byte[] blockData)
    {
        stream.Seek(0, SeekOrigin.End);
        stream.Write(blockData.LengthBytesLE(), 0, 4);
        stream.Write(blockData, 0, blockData.Length);
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
        if (blockCount == 1000)
        {
            CloseBlock(openBlockPath);
            CreateNewBlock(openBlockPath);
        }
    }

    private static void CloseBlock(string openBlockPath)
    {
        File.Move(openBlockPath, openBlockPath.Replace("_", ""));
    }

    private static void CreateNewBlock(string openBlockPath)
    {
        var newBlockNumber = int.Parse(openBlockPath.Replace("_", "")) + 1;
        using var filestream = File.Create($"_{newBlockNumber:D6}.blk");

        // Write initial block count (0)
        filestream.Write(BitConverter.GetBytes(0), 0, 4);
    }
}