using System.Text.Json;

namespace Spood.BlockChainCards.Lib;

public interface IWalletReader
{
    BCUserWallet LoadWallet(string walletPath);
    void SaveWallet(string walletPath, BCUserWallet wallet);
}
