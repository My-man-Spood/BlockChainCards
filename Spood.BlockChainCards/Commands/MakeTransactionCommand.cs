namespace Spood.BlockChainCards.Commands;

class MakeTransactionCommand : ICommand
{
    public string Name => "make-transaction";

    public void Execute(string[] options)
    {
        // Implementation for making a transaction
        // This could involve interacting with a blockchain or an API
        // to create a new transaction with the provided options.
    }
}