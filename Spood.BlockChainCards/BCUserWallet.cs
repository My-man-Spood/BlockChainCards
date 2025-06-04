using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace Spood.BlockChainCards;

class BCUserWallet
{
    public string PrivateKey64 { get; set; } = "";
    public string publicKey64 { get; set; } = "";

    [JsonIgnore]
    public byte[] PrivateKey => Convert.FromBase64String(PrivateKey64);

    [JsonIgnore]
    public byte[] PublicKey => Convert.FromBase64String(publicKey64);

    [JsonIgnore]
    public byte[] publicKeyHash => ComputeHash(PublicKey);

    [JsonIgnore]
    public string publicKeyHash64 => Convert.ToBase64String(publicKeyHash);


    public BCUserWallet()
    {
        // empty constructor for serialization
    }

    public BCUserWallet(byte[] publicKey, byte[] privateKey)
    {
        publicKey64 = Convert.ToBase64String(publicKey);
        PrivateKey64 = Convert.ToBase64String(privateKey);
    }

    public byte[] ComputeHash(byte[] bytes)
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(bytes);
    }
}