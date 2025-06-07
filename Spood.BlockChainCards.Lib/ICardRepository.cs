namespace Spood.BlockChainCards.Lib;

public interface ICardRepository
{
    void SaveCard(BCCard card);
    BCCard LoadCard(string cardName);
    bool CardExists(string cardName);
}
