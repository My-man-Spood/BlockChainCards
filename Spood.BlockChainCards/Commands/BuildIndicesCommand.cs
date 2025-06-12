using CommandLine;
using Spood.BlockChainCards.Lib;
using Spood.BlockChainCards.Lib.Configuration;
using Spood.BlockChainCards.Serialization;

namespace Spood.BlockChainCards.Commands;

public class BuildIndicesCommand : ICommand
{
    public string Name => "build-indices";
    private readonly IBlockChainReader blockChainReader;
    private readonly BlockFileIndex blockFileIndex;
    private readonly BlockFileReader blockFileReader;
    private readonly PathConfiguration pathConfig;
    private readonly SQLiteCardOwnershipStore cardOwnershipStore;

    public BuildIndicesCommand(PathConfiguration pathConfig, IBlockChainReader blockChainReader, BlockFileIndex blockFileIndex, BlockFileReader blockFileReader, SQLiteCardOwnershipStore cardOwnershipStore)
    {
        this.blockChainReader = blockChainReader;
        this.blockFileIndex = blockFileIndex;
        this.blockFileReader = blockFileReader;
        this.pathConfig = pathConfig;
        this.cardOwnershipStore = cardOwnershipStore;
    }

    public void Execute(string[] args)
    {
        Parser.Default.ParseArguments<BuildIndicesOptions>(args)
            .WithParsed(BuildIndices);
    }

    private void BuildIndices(BuildIndicesOptions options)
    {
        CheckRebuild(options);

        if (options.Blockchain || options.All)
        {
            CatchupBlockIndex();
        }

        if (options.CardOwnership || options.All)
        {
            //check if block index is up to date before catching up card ownership index
            int indexedHeight = blockFileIndex.GetTotalBlockCount();
            int totalBlocks = blockFileReader.GetTotalBlockCount();
            if (indexedHeight < totalBlocks)
            {
                Console.WriteLine("Block index is not up to date. Card ownership cannot be established.");
                Console.WriteLine("Use build-indices -b or build-indices -a to catch up block index first.");
                return;
            }
            CatchupCardOwnershipIndex();
        }
    }
    private void CheckRebuild(BuildIndicesOptions options)
    {
        if (options.Rebuild)
        {
            Console.WriteLine("Are you sure you want to rebuild index, this can take a long time.");
            Console.WriteLine("Press Y to continue or any other key to cancel.");
            if (Console.ReadLine() != "Y")
            {
                Console.WriteLine("Index rebuild cancelled.");
                return;
            }
            if(options.Blockchain || options.All)
            {
                ClearBlockIndex();
            }
            if(options.CardOwnership || options.All)
            {
                ClearCardOwnershipIndex();
            }
        }
    }
    private void ClearBlockIndex()
    {
        if(File.Exists(pathConfig.BlockIndexFile))
        {
            File.Delete(pathConfig.BlockIndexFile);
        }

        blockFileIndex.Initialize();
    }

    private void ClearCardOwnershipIndex()
    {
        if(File.Exists(pathConfig.CardOwnershipDbPath))
        {
            File.Delete(pathConfig.CardOwnershipDbPath);
        }

        cardOwnershipStore.Initialize();
    }

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

    private void CatchupCardOwnershipIndex()
    {
        var lastBlockIndex = blockFileIndex.GetTotalBlockCount() - 1;
        var safeBlockIndex = Math.Max(0, lastBlockIndex - pathConfig.BlockSafetyThreshold);
        var checkpointIndex = cardOwnershipStore.GetCheckpoint();
        if (checkpointIndex < safeBlockIndex)
        {
            BulkIngestCardOwnership();
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

    private void BulkIngestCardOwnership()
    {
        var checkpointIndex = cardOwnershipStore.GetCheckpoint();
        var lastBlockIndex = blockFileIndex.GetTotalBlockCount() - 1;
        var safeEnd = lastBlockIndex - pathConfig.BlockSafetyThreshold;

        if (safeEnd > checkpointIndex)
        {
            cardOwnershipStore.BeginBulkIngest();
            for (int i = checkpointIndex; i <= lastBlockIndex - pathConfig.BlockSafetyThreshold; i++)
            {
                var meta = blockFileIndex.LookupByHeight(i);
                var block = blockFileReader.ReadBlockDirect(meta.FilePath, meta.Offset, meta.Size);
                cardOwnershipStore.IngestBlock(block, i);
            }
            cardOwnershipStore.EndBulkIngest();
        }
    }
}

public class BuildIndicesOptions
{
    [Option('a', "all", Required = false, Default = false)]
    public bool All { get; set; }

    [Option('b', "blockchain", Required = false, Default = false)]
    public bool Blockchain { get; set; }

    [Option('c', "card-ownership", Required = false, Default = false)]
    public bool CardOwnership { get; set; }
    
    [Option('r', "rebuild", Required = false, Default = false)]
    public bool Rebuild { get; set; }
}
