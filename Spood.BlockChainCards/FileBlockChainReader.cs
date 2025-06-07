using System.Collections.Generic;
using System.Linq;
using Spood.BlockChainCards.Transactions;

namespace Spood.BlockChainCards;

public class FileBlockChainReader : IBlockChainReader
{
    private readonly string _filePath;
    public FileBlockChainReader(string filePath)
    {
        _filePath = filePath;
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

    public IReadOnlyList<BCCardBlock> ReadBlockChain()
    {
        if (!File.Exists(_filePath))
            return new List<BCCardBlock>();
        var json = File.ReadAllText(_filePath);
        return System.Text.Json.JsonSerializer.Deserialize<List<BCCardBlock>>(json);
    }

    public BCCardBlock GetLastBlock()
    {
        var blocks = ReadBlockChain();
        if (!blocks.Any())
            throw new InvalidOperationException("No blocks exist in the blockchain.");
        return blocks.Last();
    }

    public void SaveBlockChain(IEnumerable<BCCardBlock> blocks)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(blocks);
        File.WriteAllText(_filePath, json);
    }
}
