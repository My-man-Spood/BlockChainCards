using System.Text.Json;
using Spood.BlockChainCards.Lib;
using Spood.BlockChainCards.Lib.Configuration;
using Spood.BlockChainCards.Lib.Transactions;
using Spood.BlockChainCards.Serialization;

namespace Spood.BlockChainCards;

public class FileBlockChainReader : IBlockChainReader
{
    private const int blockSafetyThreshold = 5;
    private readonly string blockchainPath;
    private readonly JsonSerializerOptions serializerOptions;
    private readonly IWalletReader walletReader;
    private readonly ICardOwnershipStore cardOwnershipStore;
    private readonly BlockFileIndex blockFileIndex;
    private readonly BlockFileReader blockFileReader;
    private readonly PathConfiguration pathConfig;
    
    public FileBlockChainReader(string blockchainPath, IWalletReader walletReader, ICardOwnershipStore cardOwnershipStore, PathConfiguration pathConfig)
    {
        this.blockchainPath = blockchainPath;
        this.walletReader = walletReader;
        this.cardOwnershipStore = cardOwnershipStore;
        this.pathConfig = pathConfig;
        
        // Path is now managed by PathConfiguration
        // pathConfig.EnsureDirectoriesExist() should be called before this constructor
        
        // Robust, idempotent initialization
        this.blockFileReader = new BlockFileReader(blockchainPath);
        this.blockFileReader.Initialize();
        this.blockFileIndex = new BlockFileIndex(pathConfig);
        this.blockFileIndex.Initialize();
        
        if (blockFileReader.GetTotalBlockCount() == 0)
        {
            // Only create genesis if no blocks exist
            var genesisBlock = new BCBlock(Enumerable.Repeat((byte)153,32).ToArray(), []);
            var appendBlockResult = blockFileReader.AppendBlock(genesisBlock);
            blockFileIndex.AddBlock(genesisBlock.Hash, appendBlockResult.BlockIndexGlobal, appendBlockResult.BlockFilePath, appendBlockResult.BlockOffset, appendBlockResult.BlockSize);
        }
        else
        {
            CatchupBlockIndex();
            CatchupCardOwnership(cardOwnershipStore);
        }
    }

    /// <summary>
    /// Returns the most recent blocks from the blockchain, up to the specified count
    /// </summary>
    /// <param name="count">Maximum number of blocks to retrieve</param>
    /// <returns>Collection of the most recent blocks in descending order (newest first)</returns>
    public IEnumerable<BCBlock> GetLatestBlocks(int count)
    {
        // Get the total number of blocks to determine the starting point
        int totalBlocks = blockFileIndex.GetTotalBlockCount();
        
        if (totalBlocks == 0 || count <= 0)
            yield break;
            
        // Handle case where requested count exceeds available blocks
        int actualCount = Math.Min(count, totalBlocks);
        
        // Find the start block (count blocks from the end)
        int startHeight = Math.Max(0, totalBlocks - actualCount);
        var startMetadata = blockFileIndex.LookupByHeight(startHeight);
        
        if (startMetadata == null)
            yield break;
            
        
        // Efficiently read all blocks from the starting point
        var blocks = blockFileReader.ReadBlocksFromPoint(startMetadata);
        
        // Return in reverse order (newest first)
        for (int i = blocks.Count() - 1; i >= 0; i--)
        {
            yield return blocks.ElementAt(i);
        }
    }

    /// <summary>
    /// Catches up the block index by using BlockFileReader.EnumerateBlocksWithResults, inserting all unindexed blocks.
    /// </summary>
    private void CatchupBlockIndex()
    {
        int indexedHeight = blockFileIndex.GetTotalBlockCount();
        int totalBlocks = blockFileReader.GetTotalBlockCount();
        if (indexedHeight >= totalBlocks)
            return;

        blockFileIndex.BeginBulkIngest();
        foreach (var result in blockFileReader.EnumerateBlocksMetaData(indexedHeight))
        {
            blockFileIndex.IngestBlock(result.BlockHash, result.BlockIndexGlobal, result.BlockFilePath, result.BlockOffset, result.BlockSize);
        }
        blockFileIndex.EndBulkIngest();
    }


    private void CatchupCardOwnership(ICardOwnershipStore cardOwnershipStore)
    {
        var lastBlockIndex = blockFileIndex.GetTotalBlockCount() - 1;
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
            var meta = blockFileIndex.LookupByHeight(i);
            var block = blockFileReader.ReadBlockDirect(meta.FilePath, meta.Offset, meta.Size);
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
        var lastBlockIndex = blockFileIndex.GetTotalBlockCount() - 1;
        var safeEnd = lastBlockIndex - blockSafetyThreshold;

        if (safeEnd > checkpointIndex)
        {
            cardOwnershipStore.BeginBulkIngest();
            for (int i = checkpointIndex; i <= lastBlockIndex - blockSafetyThreshold; i++)
            {
                var meta = blockFileIndex.LookupByHeight(i);
                var block = blockFileReader.ReadBlockDirect(meta.FilePath, meta.Offset, meta.Size);
                cardOwnershipStore.IngestBlock(block, i);
            }
            cardOwnershipStore.EndBulkIngest();
        }
    }

    public void InitializeBlockChain()
    {
        blockFileIndex.Initialize();
        var genesisBlock = new BCBlock(Enumerable.Repeat((byte)153,32).ToArray(), []);
        blockFileReader.InitializeBlockChain();
        var appendBlockResult = blockFileReader.AppendBlock(genesisBlock);
        blockFileIndex.AddBlock(genesisBlock.Hash, appendBlockResult.BlockIndexGlobal, appendBlockResult.BlockFilePath, appendBlockResult.BlockOffset, appendBlockResult.BlockSize);
    }

    public void AddTransaction(BCTransaction transaction)
    {
        ValidateTransaction(transaction);
        var lastBlockIndex = blockFileIndex.GetTotalBlockCount() - 1;
        var prev = blockFileIndex.LookupByHeight(lastBlockIndex);

        var block = new BCBlock(prev.Hash, [transaction]);
        foreach (var card in transaction.GetAllCards())
        {
            cardOwnershipStore.LockCard(card, lastBlockIndex, transaction.Id);
        }
        blockFileReader.AppendBlock(block);
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
        var authorityWallet = walletReader.LoadWallet(pathConfig.AuthorityWalletPath);
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
}
