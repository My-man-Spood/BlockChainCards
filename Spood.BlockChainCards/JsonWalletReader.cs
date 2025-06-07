using System.Text.Json;
using Spood.BlockChainCards.Lib;

namespace Spood.BlockChainCards;

public class JsonWalletReader : IWalletReader
{
    private readonly JsonSerializerOptions _serializerOptions;
    public JsonWalletReader(JsonSerializerOptions serializerOptions)
    {
        _serializerOptions = serializerOptions;
    }

    public BCUserWallet LoadWallet(string walletPath)
    {
        var json = File.ReadAllText(walletPath);
        return JsonSerializer.Deserialize<BCUserWallet>(json, _serializerOptions)
            ?? throw new InvalidOperationException($"Failed to load wallet from {walletPath}");
    }

    public void SaveWallet(string walletPath, BCUserWallet wallet)
    {
        var json = JsonSerializer.Serialize(wallet, _serializerOptions);
        File.WriteAllText(walletPath, json);
    }
}
