using CommandLine;
using Spood.BlockChainCards.Lib;
using Spood.BlockChainCards.Lib.Transactions;
using System.Text.Json;

namespace Spood.BlockChainCards.Commands;

class MakeTransactionCommand : ICommand
{
    public string Name => "make-transaction";
    private readonly IBlockChainReader blockChainReader;
    private readonly IWalletReader walletReader;
    private readonly JsonSerializerOptions serializerOptions;
    public MakeTransactionCommand(IBlockChainReader blockChainReader, IWalletReader walletReader, JsonSerializerOptions serializerOptions)
    {
        this.blockChainReader = blockChainReader;
        this.walletReader = walletReader;
        this.serializerOptions = serializerOptions;
    }

    public void Execute(string[] options)
    {
        // take user1 wallet
        var makeTransactionOptions = Parser.Default.ParseArguments<MakeTransactionOptions>(options)
            .WithParsed(TakeSecondInput);
    }

    public void TakeSecondInput(MakeTransactionOptions options)
    {
        Console.WriteLine("enter arguments for user2");
        var command2 = Console.ReadLine();
        var cmdArgs = command2.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var makeTransactionOptions2 = Parser.Default.ParseArguments<MakeTransactionOptions>(cmdArgs)
            .WithParsed(opt => MakeTransaction(options, opt));
    }

    public void MakeTransaction(MakeTransactionOptions options, MakeTransactionOptions options2)
    {
        // Load the user's wallet from the provided path
        var userWallet = walletReader.LoadWallet(options.UserWalletPath);
        var recipientWallet = walletReader.LoadWallet(options2.UserWalletPath);

        // Create a new transaction
        var transaction = new TradeCardsTransaction(
            userWallet.PublicKey,
            recipientWallet.PublicKey,
            [Convert.FromHexString(options.CardHash)], // Assuming no card hashes are being transferred in this transaction
            [Convert.FromHexString(options2.CardHash)],
            DateTime.UtcNow);

        // Sign the transaction with the user's private key
        transaction.Sign(userWallet.PrivateKey);
        transaction.Sign(recipientWallet.PrivateKey);
        // Add the transaction to the blockchain
        blockChainReader.AddTransaction(transaction);
    }
}

class MakeTransactionOptions
{
    [Option('u', "user", Required = true, HelpText = "The sender's public key hash.")]
    public string UserWalletPath { get; set; } = "";

    [Option('c', "card", Required = true, HelpText = "The recipient's public key hash.")]
    public string CardHash { get; set; } = "";
}