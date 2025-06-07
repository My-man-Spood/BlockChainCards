namespace Spood.BlockChainCards.Lib;

public interface ICardOwnershipStore
{
    // --- Bulk ingest API ---
    /// <summary>
    /// Begins a bulk ingest session for efficient batch import of blocks.
    /// Opens a persistent connection and transaction for the duration of the ingest.
    /// </summary>
    void BeginBulkIngest();

    /// <summary>
    /// Applies a block's ownership changes as part of a bulk ingest session.
    /// Should be called between BeginBulkIngest and EndBulkIngest.
    /// </summary>
    void IngestBlock(BCBlock block, int blockIndex);

    /// <summary>
    /// Ends the bulk ingest session, committing all changes and closing any resources.
    /// </summary>
    void EndBulkIngest();
    /// <summary>
    /// Returns the current owner public key for a card hash, or null if not found.
    /// </summary>
    byte[]? GetOwner(byte[] cardHash);

    /// <summary>
    /// Locks a card (used when processing new blocks).
    /// </summary>
    void LockCard(byte[] cardHash, int blockIndex, string? transactionId = null);

    /// <summary>
    /// Unlocks a card (used when processing new blocks).
    /// </summary>
    void UnlockCard(byte[] cardHash);

    /// <summary>
    /// Sets the owner for a card hash (used when processing new blocks).
    /// </summary>
    void SetOwner(byte[] cardHash, byte[] ownerPublicKey);

    /// <summary>
    /// Returns the last block index processed (checkpoint).
    /// </summary>
    int GetCheckpoint();

    /// <summary>
    /// Sets the last block index processed (checkpoint).
    /// </summary>
    void SetCheckpoint(int blockIndex);

    /// <summary>
    /// Processes a block and updates ownership and checkpoint accordingly.
    /// </summary>
    void UpdateOwnershipForBlock(BCBlock block, int blockIndex);
}
