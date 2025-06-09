using Spood.BlockChainCards.Lib.ByteUtils;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace Spood.BlockChainCards.Lib;

public class BCUserWallet
{
    public byte[] PrivateKey { get; init; }
    public byte[] PublicKey { get; init; }
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