namespace Spood.BlockChainCards;

public class BlockIndexMetaData
{
    public byte[] Hash { get; }
    public string FilePath { get; }
    public int GlobalIndex { get; }
    public int Offset { get; }
    public int Size { get; }

    public BlockIndexMetaData(byte[] hash, string filePath, int globalIndex, int offset, int size)
    {
        Hash = hash;
        FilePath = filePath;
        GlobalIndex = globalIndex;
        Offset = offset;
        Size = size;
    }
}