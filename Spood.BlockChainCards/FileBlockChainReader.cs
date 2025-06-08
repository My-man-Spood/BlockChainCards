using System.Text.Json;
using Spood.BlockChainCards.Lib;
using Spood.BlockChainCards.Lib.Transactions;

namespace Spood.BlockChainCards;

public class FileBlockChainReader : IBlockChainReader
{
    private const int blockSafetyThreshold = 5;
    private readonly string filePath;
    private readonly JsonSerializerOptions serializerOptions;
    private readonly IWalletReader walletReader;
    private readonly ICardOwnershipStore cardOwnershipStore;
    public FileBlockChainReader(string filePath, JsonSerializerOptions serializerOptions, IWalletReader walletReader, ICardOwnershipStore cardOwnershipStore)
    {
        this.filePath = filePath;
        this.serializerOptions = serializerOptions;
        this.walletReader = walletReader;
        this.cardOwnershipStore = cardOwnershipStore;
        if (!File.Exists(filePath))
        {
            InitializeBlockChain();
        }
        else
        {
            CatchupCardOwnership(cardOwnershipStore);
        }
    }

    private void CatchupCardOwnership(ICardOwnershipStore cardOwnershipStore)
    {
        var blocks = ReadBlockChain();
        var lastBlockIndex = blocks.Count - 1;
        var safeBlockIndex = Math.Max(0, lastBlockIndex - blockSafetyThreshold);
        var checkpointIndex = cardOwnershipStore.GetCheckpoint();
        if (checkpointIndex < safeBlockIndex)
        {
            BulkIngest();
        }
        checkpointIndex = cardOwnershipStore.GetCheckpoint();
        
        // Lock cards for unsafe blocks
        for (int i = checkpointIndex + 1; i <= lastBlockIndex; i++)
        {
            var block = blocks[i];
            foreach (var tx in block.Transactions)
            {
                foreach (var card in tx.GetAllCards())
                {
                    cardOwnershipStore.LockCard(card, i, tx.Id);
                }
            }
        }
    }

    private void BulkIngest()
    {
        var checkpointIndex = cardOwnershipStore.GetCheckpoint();
        var lastBlockIndex = GetLastBlockIndex();
        var blocks = ReadBlockChain();
        if (lastBlockIndex - blockSafetyThreshold > checkpointIndex)
        {
            cardOwnershipStore.BeginBulkIngest();
            for (int i = checkpointIndex; i <= lastBlockIndex - blockSafetyThreshold; i++)
            {
                cardOwnershipStore.IngestBlock(blocks[i], i);
            }
            cardOwnershipStore.EndBulkIngest();
        }
    }

    public void InitializeBlockChain()
    {
        var genesisBlock = new BCBlock(Enumerable.Repeat((byte)153,32).ToArray(), []);
        var blockChain = new List<BCBlock> { genesisBlock };

        var blockChainJson = JsonSerializer.Serialize(blockChain, serializerOptions);
        File.WriteAllText(filePath, blockChainJson);
    }

    public void AddTransaction(BCTransaction transaction)
    {
        ValidateTransaction(transaction);
        var blocks = ReadBlockChain().ToList();
        var lastBlockIndex = GetLastBlockIndex();
        foreach (var card in transaction.GetAllCards())
        {
            cardOwnershipStore.LockCard(card, lastBlockIndex, transaction.Id);
        }
        SaveBlockChain(blocks);
    }

    private bool ValidateTransaction(BCTransaction transaction)
    {
        switch (transaction)
        {
            case MintCardTransaction mintCardTransaction:
                return ValidateMintCardTransaction(mintCardTransaction);
            case TradeCardsTransaction tradeCardsTransaction:
                return ValidateTradeCardsTransaction(tradeCardsTransaction);
            default:
                return false;
        }
    }

    private bool ValidateMintCardTransaction(MintCardTransaction transaction)
    {
        var authorityWallet = walletReader.LoadWallet("./Authority-wallet.json");
        var isAuthorityPublicKey = transaction.AuthorityPublicKey.SequenceEqual(authorityWallet.PublicKey);

        return isAuthorityPublicKey && transaction.VerifySignature();
    }

    private bool ValidateTradeCardsTransaction(TradeCardsTransaction transaction)
    {
        var user1CardsValid = ValidateCardsForUser(transaction.CardsFromUser1, transaction.User1PublicKey);
        var user2CardsValid = ValidateCardsForUser(transaction.CardsFromUser2, transaction.User2PublicKey);
        return transaction.VerifySignature() && user1CardsValid && user2CardsValid;
    }

    private bool ValidateCardsForUser(IEnumerable<byte[]> cards, byte[] publicKey)
    {
        foreach (var card in cards)
        {
            var owner = cardOwnershipStore.GetOwner(card);
            if (owner == null || !owner.SequenceEqual(publicKey))
                return false;
        }
        return true;
    }

    public IReadOnlyList<BCBlock> ReadBlockChain()
    {
        if (!File.Exists(filePath))
            return new List<BCBlock>();
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<List<BCBlock>>(json, serializerOptions)!;
    }

    public int GetLastBlockIndex()
    {
        var blocks = ReadBlockChain();
        if (!blocks.Any())
            throw new InvalidOperationException("No blocks exist in the blockchain.");
        return blocks.Count - 1;
    }

    public void SaveBlockChain(IEnumerable<BCBlock> blocks)
    {
        var json = JsonSerializer.Serialize(blocks, serializerOptions);
        File.WriteAllText(filePath, json);
    }
}
