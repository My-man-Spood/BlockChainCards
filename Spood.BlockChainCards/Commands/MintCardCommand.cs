using CommandLine;
using Spood.BlockChainCards.Lib;
using Spood.BlockChainCards.Lib.Transactions;
using System.Text.Json;

namespace Spood.BlockChainCards.Commands;

class MintCardCommand : ICommand
{
    public string Name => "mint-card";

    private readonly ICardRepository cardRepo;
    private readonly IBlockChainReader blockChainReader;
    private readonly IWalletReader walletReader;
    private readonly JsonSerializerOptions serializerOptions;

    public MintCardCommand(ICardRepository cardRepo, IBlockChainReader blockChainReader, IWalletReader walletReader, JsonSerializerOptions serializerOptions)
    {
        this.cardRepo = cardRepo;
        this.blockChainReader = blockChainReader;
        this.walletReader = walletReader;
        this.serializerOptions = serializerOptions;
    }

    public void Execute(string[] options)
    {
        var mintCardOptions = Parser.Default.ParseArguments<MintCardOptions>(options)
            .WithParsed(MintCard);
    }

    private void MintCard(MintCardOptions options)
    {
        var card1 = new BCCard(options.CardName);
        cardRepo.SaveCard(card1);

        var userWallet = walletReader.LoadWallet(options.UserKeyPath);
        var authorityWallet = walletReader.LoadWallet("./Authority-wallet.json");

        var transaction = new MintCardTransaction(
            authorityWallet.PublicKey,
            userWallet.PublicKey,
            card1.Hash,
            DateTime.UtcNow);

        transaction.Sign(authorityWallet.PrivateKey);
        blockChainReader.AddTransaction(transaction);
    }

}

public class MintCardOptions
{
    [Option('u', "user", Required = true, HelpText = "The user ID of the card owner.")]
    public string UserKeyPath { get; set; } = "";

    [Option('n', "name", Required = false, HelpText = "The name of the card.")]
    public string CardName { get; set; } = "";
}