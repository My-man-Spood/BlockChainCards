using Spood.BlockChainCards.Lib.Transactions;

namespace Spood.BlockChainCards.Lib;

public interface IBlockChainReader
{
    void AddTransaction(BCTransaction transaction);
    
    IEnumerable<BCBlock> GetLatestBlocks(int count);
}
