using System.Security.Cryptography;
using Spood.BlockChainCards.Lib;
using System.Text.Json;
using CommandLine;

namespace Spood.BlockChainCards.Commands;

public class CreateUserWalletCommand : ICommand
{
    public string Name => "create-wallet";
    private readonly JsonSerializerOptions serializerOptions;
    private readonly IWalletReader walletReader;
    public CreateUserWalletCommand(JsonSerializerOptions serializerOptions, IWalletReader walletReader)
    {
        this.serializerOptions = serializerOptions;
        this.walletReader = walletReader;
    }

    public void Execute(string[] options)
    {
        var createUserOptions = Parser.Default.ParseArguments<CreateUserOptions>(options)
        .WithParsed(CreateWallet);
    }

    private void CreateWallet(CreateUserOptions options)
    {
        // Logic to create a user with the provided options
        // This could include saving user details to a database or initializing user state
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        
        var privateKey = ecdsa.ExportECPrivateKey();
        var publicKey = ecdsa.ExportSubjectPublicKeyInfo();
        // Save these as needed
        var userWallet = new BCUserWallet(publicKey, privateKey);
        walletReader.SaveWallet(Path.Combine(options.KeyPath, $"{options.KeyName}.json"), userWallet);

        Console.WriteLine($"User wallet created. Saved to {options.KeyPath}.");
    }
}

public class CreateUserOptions
{
    [Option('n', "name", Required = true, HelpText = "The name of the user to create.")]
    public string KeyName { get; set; } = "";

    [Option('p', "path", Required = true, HelpText = "The secret key for the user.")]
    public string KeyPath { get; set; } = "";
}