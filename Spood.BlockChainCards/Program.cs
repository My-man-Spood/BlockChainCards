using System.Text.Json;
using Spood.BlockChainCards;
using Spood.BlockChainCards.Commands;
using Spood.BlockChainCards.Lib.ByteUtils;
using Spood.BlockChainCards.Lib.Configuration;

// Centralized path configuration
var pathConfig = new PathConfiguration("./CLI");
pathConfig.EnsureDirectoriesExist();

// Setup serializer options
var serializerOptions = new JsonSerializerOptions
{
    IgnoreReadOnlyProperties = true,
    WriteIndented = true,
};

// Directories are created by PathConfiguration

// Setup dependencies
var cardRepo = new FileCardRepository(pathConfig.CardsJsonPath, serializerOptions);
var cardOwnerShipStore = new SQLiteCardOwnershipStore(pathConfig.CardOwnershipDbPath);
var walletReader = new JsonWalletReader(serializerOptions);
// Pass PathConfiguration to the reader for consistent path management
var blockChainReader = new FileBlockChainReader(pathConfig.BlockchainPath, walletReader, cardOwnerShipStore, pathConfig);

// Register command factories
var commandFactories = new Dictionary<string, Func<ICommand>>(StringComparer.OrdinalIgnoreCase)
{
    ["mint-card"] = () => new MintCardCommand(cardRepo, blockChainReader, walletReader, serializerOptions),
    ["make-transaction"] = () => new MakeTransactionCommand(blockChainReader, walletReader, serializerOptions),
    ["show-blockchain"] = () => new ShowBlockchainCommand(blockChainReader),
    ["create-wallet"] = () => new CreateUserWalletCommand(serializerOptions, walletReader),
};

Console.WriteLine("Spood's Blockchain Cards CLI V0.1");
PrintAvailableCommands();
string command = "";
while (command != "exit")
{
    command = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(command))
        continue;

    var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var cmdName = parts[0];
    var cmdArgs = parts.Skip(1).ToArray();

    if (cmdName.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }
    else if (cmdName.Equals("help", StringComparison.OrdinalIgnoreCase))
    {
        PrintAvailableCommands();
    }
    else if (commandFactories.TryGetValue(cmdName, out var factory))
    {
        var cmd = factory();
        cmd.Execute(cmdArgs);
    }
    else
    {
        Console.WriteLine($"Unknown command: {cmdName}");
    }
}

void PrintAvailableCommands()
{
    Console.WriteLine("Available commands: " + string.Join(", ", commandFactories.Keys) + ", exit, help");
}

