using System.Collections.Generic;
using System.Linq;
using Spood.BlockChainCards.Transactions;

namespace Spood.BlockChainCards;

public class InMemoryBlockChainReader : IBlockChainReader
{
    private readonly List<BCCardBlock> _blocks = new();

    public void AddTransaction(BCTransaction transaction)
    {
        if (!_blocks.Any())
            throw new InvalidOperationException("No blocks exist in the blockchain.");
        var lastBlock = _blocks.Last();
        // You may want to create a new block or just add to the last block, depending on your design
        lastBlock.Transactions.Add(transaction);
    }

    public IReadOnlyList<BCCardBlock> ReadBlockChain()
    {
        return _blocks.AsReadOnly();
    }

    public BCCardBlock GetLastBlock()
    {
        if (!_blocks.Any())
            throw new InvalidOperationException("No blocks exist in the blockchain.");
        return _blocks.Last();
    }

    public void SaveBlockChain(IEnumerable<BCCardBlock> blocks)
    {
        _blocks.Clear();
        _blocks.AddRange(blocks);
    }
}
