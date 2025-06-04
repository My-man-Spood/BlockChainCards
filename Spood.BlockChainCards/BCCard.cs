using System.Security.Cryptography;

namespace Spood.BlockChainCards;

public class BCCard
{
    public byte[] Hash { get; }
    public string Name { get; }
    public string Hash64 => Convert.ToBase64String(Hash);

    public BCCard(string name)
    {
        Name = name;
        Hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(name));
    }
}