using Spood.BlockChainCards.Serialization;
using Spood.BlockChainCards.Lib;
using Spood.BlockChainCards.Lib.Configuration;
using Spood.BlockChainCards.Lib.Transactions;

namespace Spood.BlockChainCards.Commands;

public class InitializationCommand : ICommand
{
    private readonly PathConfiguration pathConfig;
    private readonly FileBlockChainReader fileBlockChainReader;
    public string Name => "initialize";

    public InitializationCommand(PathConfiguration pathConfig, FileBlockChainReader blockChainReader)
    {
        this.pathConfig = pathConfig;
        this.fileBlockChainReader = blockChainReader;
    }

    public void Execute(string[] options)
    {
        // 1. Create required directories
        pathConfig.EnsureDirectoriesExist();
        fileBlockChainReader.Initialize();
        // 2. Create genesis block file if it doesn't exist
        var genesisBlockFile = Path.Combine(pathConfig.BlockchainPath, "_000000.blk");
        if (!File.Exists(genesisBlockFile))
        {
            // Write block count header (1 block)
            using (var fs = new FileStream(genesisBlockFile, FileMode.CreateNew, FileAccess.Write))
            {
                var blockCountBytes = BitConverter.GetBytes(1);
                fs.Write(blockCountBytes, 0, blockCountBytes.Length);

                // Create the genesis block (customize as needed)
                var genesisBlock = new BCBlock
                {
                    PreviousHash = new byte[32],
                    Transactions = new System.Collections.Generic.List<BCTransaction>(),
                    Timestamp = DateTime.UtcNow
                };
                var serializedBlock = BlockSerializer.Serialize(genesisBlock);
                fs.Write(serializedBlock, 0, serializedBlock.Length);
            }
        }

        // 3. Create config file if needed (optional)
        // var configFile = Path.Combine(_pathConfig.RootPath, "config.json");
        // if (!File.Exists(configFile))
        // {
        //     File.WriteAllText(configFile, "{ /* default config here */ }");
        // }
    }
}

public class InitializationCommandOptions
{
    
}