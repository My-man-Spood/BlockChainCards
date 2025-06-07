using System.Text.Json;
using Spood.BlockChainCards.Lib;

namespace Spood.BlockChainCards;

public class FileCardRepository : ICardRepository
{
    private readonly string filePath;
    private readonly JsonSerializerOptions serializerOptions;
    public FileCardRepository(string filePath, JsonSerializerOptions serializerOptions)
    {
        this.filePath = filePath;
        this.serializerOptions = serializerOptions;
    }

    public void SaveCard(BCCard card)
    {
        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, "[]");
        }

        var cardsJson = File.ReadAllText(filePath);
        var cards = JsonSerializer.Deserialize<List<BCCard>>(cardsJson, serializerOptions);
        cards.Add(card);
        File.WriteAllText(filePath, JsonSerializer.Serialize(cards, serializerOptions));
    }

    public BCCard LoadCard(string cardName)
    {
        var cardsJson = File.ReadAllText(filePath);
        var cards = JsonSerializer.Deserialize<List<BCCard>>(cardsJson, serializerOptions);
        return cards!.FirstOrDefault(c => c.Name == cardName);
    }

    public bool CardExists(string cardName)
    {
        var cardsJson = File.ReadAllText(filePath);
        var cards = JsonSerializer.Deserialize<List<BCCard>>(cardsJson, serializerOptions);
        return cards!.Any(c => c.Name == cardName);
    }
}
