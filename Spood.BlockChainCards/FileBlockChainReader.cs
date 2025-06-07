using System.Text.Json;
using Spood.BlockChainCards.Lib;
using Spood.BlockChainCards.Lib.Transactions;

namespace Spood.BlockChainCards;

public class FileBlockChainReader : IBlockChainReader
{
    private readonly string filePath;
    private readonly JsonSerializerOptions serializerOptions;
    public FileBlockChainReader(string filePath, JsonSerializerOptions serializerOptions)
    {
        this.filePath = filePath;
        this.serializerOptions = serializerOptions;
        if (!File.Exists(filePath))
        {
            InitializeBlockChain();
        }
    }

    public void InitializeBlockChain()
    {
        var genesisBlock = new BCBlock(Enumerable.Repeat((byte)153,32).ToArray(), []);
        var blockChain = new List<BCBlock> { genesisBlock };

        var blockChainJson = JsonSerializer.Serialize(blockChain, serializerOptions);
        File.WriteAllText(filePath, blockChainJson);
    }

    public void AddTransaction(BCTransaction transaction)
    {
        var blocks = ReadBlockChain().ToList();
        if (!blocks.Any())
            throw new InvalidOperationException("No blocks exist in the blockchain.");
        var lastBlock = blocks.Last();
        lastBlock.Transactions.Add(transaction);
        SaveBlockChain(blocks);
    }

    public IReadOnlyList<BCBlock> ReadBlockChain()
    {
        if (!File.Exists(filePath))
            return new List<BCBlock>();
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<List<BCBlock>>(json, serializerOptions)!;
    }

    public BCBlock GetLastBlock()
    {
        var blocks = ReadBlockChain();
        if (!blocks.Any())
            throw new InvalidOperationException("No blocks exist in the blockchain.");
        return blocks.Last();
    }

    public void SaveBlockChain(IEnumerable<BCBlock> blocks)
    {
        var json = JsonSerializer.Serialize(blocks, serializerOptions);
        File.WriteAllText(filePath, json);
    }
}
