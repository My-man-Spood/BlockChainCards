using System.Text.Json;

namespace Spood.BlockChainCards;

public class BCCardRepo
{
    private const string path = "./cards.json";

    public static void SaveCard(BCCard card)
    {
        var cards = LoadCards();
        cards.Add(card.Hash64, card);
        File.WriteAllText(path, JsonSerializer.Serialize(cards, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static Dictionary<string, BCCard> LoadCards()
    {
        if (!File.Exists(path))
        {
            return new Dictionary<string, BCCard>();
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Dictionary<string, BCCard>>(json) ?? new Dictionary<string, BCCard>();
    }
}