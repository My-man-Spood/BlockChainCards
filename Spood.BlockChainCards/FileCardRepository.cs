namespace Spood.BlockChainCards;

public class FileCardRepository : ICardRepository
{
    private readonly string _basePath;
    public FileCardRepository(string basePath)
    {
        _basePath = basePath;
    }

    public void SaveCard(BCCard card)
    {
        var path = Path.Combine(_basePath, card.Name + ".json");
        File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(card));
    }

    public BCCard LoadCard(string cardName)
    {
        var path = Path.Combine(_basePath, cardName + ".json");
        if (!File.Exists(path))
            throw new FileNotFoundException($"Card '{cardName}' not found on disk.");
        return System.Text.Json.JsonSerializer.Deserialize<BCCard>(File.ReadAllText(path));
    }

    public bool CardExists(string cardName)
    {
        var path = Path.Combine(_basePath, cardName + ".json");
        return File.Exists(path);
    }
}
