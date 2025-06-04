using System.Security.Cryptography;
using CommandLine;

namespace Spood.BlockChainCards.Commands;

public class CreateUserWalletCommand : ICommand
{
    public string Name => "create-wallet";

    public void Execute(string[] options)
    {
        var createUserOptions = Parser.Default.ParseArguments<CreateUserOptions>(options).WithParsed(o => CreateWallet(o));
        // Implementation for creating a user
        // This could involve interacting with a database or an API
        // to create a new user with the provided options.
        
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
        var serializerOptions = new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
        };
        var walletJson = System.Text.Json.JsonSerializer.Serialize(userWallet, serializerOptions);

        var walletPath = Path.Combine(options.KeyPath, $"{options.KeyName}.json");
        File.WriteAllBytes(walletPath, System.Text.Encoding.UTF8.GetBytes(walletJson));

        Console.WriteLine($"User wallet created. Saved to {walletPath}.");
    }
}

public class CreateUserOptions
{
    [Option('n', "name", Required = true, HelpText = "The name of the user to create.")]
    public string KeyName { get; set; } = "";

    [Option('p', "path", Required = true, HelpText = "The secret key for the user.")]
    public string KeyPath { get; set; } = "";
}