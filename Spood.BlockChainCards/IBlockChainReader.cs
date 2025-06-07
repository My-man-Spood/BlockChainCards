using System.Collections.Generic;
using Spood.BlockChainCards.Transactions;

namespace Spood.BlockChainCards;

public interface IBlockChainReader
{
    void AddTransaction(BCTransaction transaction);
    IReadOnlyList<BCCardBlock> ReadBlockChain();
    BCCardBlock GetLastBlock();
    void SaveBlockChain(IEnumerable<BCCardBlock> blocks);
}
