using Spood.BlockChainCards.Lib;
using System.Collections.Concurrent;

namespace Spood.BlockChainCards;

public class InMemoryCardRepository : ICardRepository
{
    private readonly ConcurrentDictionary<string, BCCard> _cards = new();

    public void SaveCard(BCCard card)
    {
        _cards[card.Name] = card;
    }

    public BCCard LoadCard(string cardName)
    {
        if (!_cards.TryGetValue(cardName, out var card))
            throw new FileNotFoundException($"Card '{cardName}' not found in memory repository.");
        return card;
    }

    public bool CardExists(string cardName)
    {
        return _cards.ContainsKey(cardName);
    }
}
