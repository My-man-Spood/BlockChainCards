using Spood.BlockChainCards.Lib.Transactions;

namespace Spood.BlockChainCards.Lib;

public interface IBlockChainReader
{
    void AddTransaction(BCTransaction transaction);
    IReadOnlyList<BCBlock> ReadBlockChain();
    BCBlock GetLastBlock();
    void SaveBlockChain(IEnumerable<BCBlock> blocks);
}
