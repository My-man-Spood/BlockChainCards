using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace Spood.BlockChainCards;

class BCUserWallet
{
    public byte[] PrivateKey { get; init; }
    public byte[] PublicKey { get; init; }

    [JsonIgnore]
    public byte[] PublicKeyHash => SHA256.HashData(PublicKey);

    public BCUserWallet()
    {
        // empty constructor for serialization
    }

    public BCUserWallet(byte[] publicKey, byte[] privateKey)
    {
        PublicKey = publicKey;
        PrivateKey = privateKey;
    }
}