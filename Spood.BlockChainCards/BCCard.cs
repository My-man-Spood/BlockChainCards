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
        using var sha256 = SHA256.Create();
        Hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(name));
    }
}