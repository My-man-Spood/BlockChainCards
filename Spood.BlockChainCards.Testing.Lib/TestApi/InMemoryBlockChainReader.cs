using Spood.BlockChainCards.Lib;
using Spood.BlockChainCards.Lib.Transactions;

namespace Spood.BlockChainCards.Testing.Lib.TestApi;

public class InMemoryBlockChainReader : IBlockChainReader
{
    private readonly List<BCBlock> _blocks = new();

    public void AddTransaction(BCTransaction transaction)
    {
        if (!_blocks.Any())
            throw new InvalidOperationException("No blocks exist in the blockchain.");
        var lastBlock = _blocks.Last();
        // You may want to create a new block or just add to the last block, depending on your design
        lastBlock.Transactions.Add(transaction);
    }

    public IReadOnlyList<BCBlock> ReadBlockChain()
    {
        return _blocks.AsReadOnly();
    }

    public BCBlock GetLastBlock()
    {
        if (!_blocks.Any())
            throw new InvalidOperationException("No blocks exist in the blockchain.");
        return _blocks.Last();
    }

    public void SaveBlockChain(IEnumerable<BCBlock> blocks)
    {
        _blocks.Clear();
        _blocks.AddRange(blocks);
    }
}
