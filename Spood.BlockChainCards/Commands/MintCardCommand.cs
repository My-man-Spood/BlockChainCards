using CommandLine;

namespace Spood.BlockChainCards.Commands;

class MintCardCommand : ICommand
{
    public string Name => "mint-card";

    public void Execute(string[] options)
    {
        var mintCardOptions = Parser.Default.ParseArguments<MintCardOptions>(options)
            .WithParsed(o => MintCard(o));
    }

    private void MintCard(MintCardOptions options)
    {
        var card1 = new BCCard(options.CardName);
        BCCardRepo.SaveCard(new BCCard(options.CardName));

        var userWalletjson = File.ReadAllText(options.UserKeyPath);
        var userWallet = System.Text.Json.JsonSerializer.Deserialize<BCUserWallet>(userWalletjson);

        var authorityWalletJson = File.ReadAllText("./Authority-wallet.json");
        var authorityWallet = System.Text.Json.JsonSerializer.Deserialize<BCUserWallet>(authorityWalletJson);

        var transaction = new BCTransaction(
            authorityWallet.PublicKeyHash,
            userWallet.PublicKeyHash,
            [card1.Hash],
            [],
            DateTime.UtcNow,
            BCTransactionType.MintCard);

        transaction.Sign(authorityWallet.PrivateKey);
        BlockChainReader.AddTransaction(transaction);
    }

}

public class MintCardOptions
{
    [Option('u', "user", Required = true, HelpText = "The user ID of the card owner.")]
    public string UserKeyPath { get; set; } = "";

    [Option('n', "name", Required = false, HelpText = "The name of the card.")]
    public string CardName { get; set; } = "";
}