namespace Spood.BlockChainCards.Commands;

class ShowBlockchainCommand : ICommand
{
    public string Name => "show-blockchain";

    public void Execute(string[] options)
    {
        // Implementation for showing the blockchain
        // This could involve fetching and displaying the current state of the blockchain,
        // including blocks, transactions, and other relevant information.
    }
}