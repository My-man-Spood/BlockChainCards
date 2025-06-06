using CommandLine;

namespace Spood.BlockChainCards.Commands;

class MakeTransactionCommand : ICommand
{
    public string Name => "make-transaction";

    public void Execute(string[] options)
    {
        // take user1 wallet
        var makeTransactionOptions = Parser.Default.ParseArguments<MakeTransactionOptions>(options)
            .WithParsed(o => TakeSecondInput(o));
    }

    public void TakeSecondInput(MakeTransactionOptions options)
    {
        Console.WriteLine("enter arguments for user2");
        var command2 = Console.ReadLine();
        var cmdArgs = command2.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var makeTransactionOptions2 = Parser.Default.ParseArguments<MakeTransactionOptions>(cmdArgs)
            .WithParsed(options2 => MakeTransaction(options, options2));
    }

    public void MakeTransaction(MakeTransactionOptions options, MakeTransactionOptions options2)
    {
        // Load the user's wallet from the provided path
        var userWalletJson = File.ReadAllText(options.UserWalletPath);
        var userWallet = System.Text.Json.JsonSerializer.Deserialize<BCUserWallet>(userWalletJson);

        // Load the recipient's wallet from the provided card hash
        var recipientWalletJson = File.ReadAllText(options.CardHash);
        var recipientWallet = System.Text.Json.JsonSerializer.Deserialize<BCUserWallet>(recipientWalletJson);

        // Create a new transaction
        var transaction = new BCTransaction(
            userWallet.PublicKeyHash,
            recipientWallet.PublicKeyHash,
            [options.CardHash], // Assuming no card hashes are being transferred in this transaction
            [options2.CardHash],
            DateTime.UtcNow,
            BCTransactionType.TransferCard);

        // Sign the transaction with the user's private key
        transaction.Sign(userWallet.PrivateKey);
        transaction.Sign(recipientWallet.PrivateKey);
        // Add the transaction to the blockchain
        BlockChainReader.AddTransaction(transaction);
    }
}

class MakeTransactionOptions
{
    [Option('u', "user", Required = true, HelpText = "The sender's public key hash.")]
    public string UserWalletPath { get; set; } = "";

    [Option('c', "card", Required = true, HelpText = "The recipient's public key hash.")]
    public string CardHash { get; set; } = "";
}