using System.Text.Json;
using CommandLine;
using Spood.BlockChainCards.Lib;
using Spood.BlockChainCards.Lib.Transactions;
using Spood.BlockChainCards.Lib.Utils;

namespace Spood.BlockChainCards.Commands;

class ShowBlockchainCommand : ICommand
{
    public string Name => "show-blockchain";

    private readonly IBlockChainReader blockChainReader;
    public ShowBlockchainCommand(IBlockChainReader blockChainReader)
    {
        this.blockChainReader = blockChainReader;
    }

    public void Execute(string[] options)
    {
        var showBlockChainOptions = Parser.Default.ParseArguments<ShowBlockChainCommandOptions>(options)
            .WithParsed(ShowBlockChain);
    }

    private void ShowBlockChain(ShowBlockChainCommandOptions options)
    {
        var blocks = blockChainReader.ReadBlockChain();
        var count = Math.Min(options.Count, blocks.Count);
        for (int i = blocks.Count - count; i < blocks.Count; i++)
        {
            var block = blocks[i];
            if(options.Verbose)
                PrintBlockVerbose(block, i);
            else
                PrintBlockShort(block, i);

            if (options.ShowTransactions)
            {
                for (int j = 0; j < block.Transactions.Count; j++)
                {
                    if (j != 0) Console.WriteLine("");
                    if (options.Verbose)
                        PrintTransactionVerbose(block.Transactions[j]);
                    else
                        PrintTransactionShort(block.Transactions[j]);
                }
            }
        }
    }

    private static void PrintTransactionVerbose(BCTransaction transaction)
    {
        switch (transaction)
        {
            case MintCardTransaction mintCardTransaction:
                PrintMintCardTransactionVerbose(mintCardTransaction);
                break;
            case TradeCardsTransaction tradeCardsTransaction:
                PrintTradeCardsTransactionVerbose(tradeCardsTransaction);
                break;
        }
    }

    private static void PrintMintCardTransactionVerbose(MintCardTransaction transaction)
    {
        Console.WriteLine("-    Type=MintCard");
        Console.WriteLine($"-    Authority={transaction.AuthorityPublicKey.ToHex()}");
        Console.WriteLine($"-    Recipient={transaction.RecipientPublicKey.ToHex()}");
        Console.WriteLine($"-    Card={transaction.Card.ToHex()}");
        Console.WriteLine($"-    Timestamp={transaction.Timestamp:O}");
    }

    private static void PrintTradeCardsTransactionVerbose(TradeCardsTransaction transaction)
    {
        Console.WriteLine("-    Type=TradeCards");
        Console.WriteLine($"-    User1={transaction.User1PublicKey.ToHex()}");
        Console.WriteLine($"-    User2={transaction.User2PublicKey.ToHex()}");
        for (int i = 0; i < transaction.CardsFromUser1.Count(); i++)
        {
            Console.WriteLine($"-    -    CardFromUser1[{i}]={transaction.CardsFromUser1.ElementAt(i).ToHex()}");
        }
        for (int i = 0; i < transaction.CardsFromUser2.Count(); i++)
        {
            Console.WriteLine($"-    -    CardFromUser2[{i}]={transaction.CardsFromUser2.ElementAt(i).ToHex()}");
        }
        Console.WriteLine($"-    Timestamp={transaction.Timestamp:O}");
    }

    private static void PrintTransactionShort(BCTransaction transaction)
    {
        Console.WriteLine($"Transaction={transaction.ToTransactionString()}");
    }

    private static void PrintBlockShort(BCBlock block, int index)
    {
        var line = $"Block={index} ";
        line += $"Hash={block.Hash.Take(8).ToArray().ToHex()} ";
        line += $"Prev={block.PreviousHash.Take(8).ToArray().ToHex()} ";
        line += $"Timestamp={block.Timestamp} ";
        line += $"Tx={block.Transactions.Count}";
        Console.WriteLine(line);
    }

    private static void PrintBlockVerbose(BCBlock block, int index)
    {
        Console.WriteLine($"Block {index}");
        Console.WriteLine($"Timestamp: {block.Timestamp}");
        Console.WriteLine($"Hash: {block.Hash.ToHex()}");
        Console.WriteLine($"Previous Hash: {block.PreviousHash.ToHex()}");
        Console.WriteLine($"Nonce: {block.Nonce}");
        Console.WriteLine($"Transactions: {block.Transactions.Count}");
    }
}

class ShowBlockChainCommandOptions
{
    [Option('t', "transactions", Required = false, Default = false, HelpText = "Show transactions in the blocks")]
    public bool ShowTransactions { get; set; }

    [Option('c', "count", Required = false, Default = 10, HelpText = "Number of blocks to show")]
    public int Count { get; set; }

    [Option('v', "verbose", Required = false, Default = false, HelpText = "Show verbose output")]
    public bool Verbose { get; set; }
}