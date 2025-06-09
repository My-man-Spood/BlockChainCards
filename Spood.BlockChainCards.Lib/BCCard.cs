using Spood.BlockChainCards.Lib.ByteUtils;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace Spood.BlockChainCards.Lib;

public class BCCard
{
    public byte[] Hash { get; }
    public string Name { get; }
    public string HashHex => Hash.ToHex();

    public BCCard(string name)
    {
        Name = name;
        Hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(name));
    }
}