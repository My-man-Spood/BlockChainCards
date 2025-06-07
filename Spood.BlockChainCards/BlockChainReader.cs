using Spood.BlockChainCards.Transactions;

namespace Spood.BlockChainCards;

public class BlockChainReader
{
    public static void AddTransaction(BCTransaction transaction)
    {
        if (!transaction.IsFullySigned) throw new InvalidOperationException("Transaction must be signed before adding to the blockchain.");

        var blockChain = ReadBlockChain();
        var lastBlock = blockChain.Last();

        var block = new BCCardBlock(lastBlock.Hash, [transaction]);
        blockChain.Add(block);

        var newBlockChainJson = System.Text.Json.JsonSerializer.Serialize(blockChain, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText("./blockchain.json", newBlockChainJson);
        Console.WriteLine("Transaction added to the blockchain.");
    }

    public static List<BCCardBlock> ReadBlockChain()
    {
        if (!File.Exists("./blockchain.json"))
        {
            Console.WriteLine("Blockchain file not found. Initializing a new blockchain.");
            InitializeBlockChain();
        }

        var blockChainJson = File.ReadAllText("./blockchain.json");
        var blockChain = System.Text.Json.JsonSerializer.Deserialize<List<BCCardBlock>>(blockChainJson)!;

        return blockChain;
    }

    public static void InitializeBlockChain()
    {
        var genesisBlock = new BCCardBlock(new byte[32], []);
        var blockChain = new List<BCCardBlock> { genesisBlock };

        var blockChainJson = System.Text.Json.JsonSerializer.Serialize(blockChain, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText("./blockchain.json", blockChainJson);
        Console.WriteLine("Blockchain initialized with genesis block.");
    }
}