using Spood.BlockChainCards.Lib;
using Spood.BlockChainCards.Lib.Transactions;
using Microsoft.Data.Sqlite;
using Spood.BlockChainCards.Lib.Utils;

namespace Spood.BlockChainCards
{
    public class SQLiteCardOwnershipStore : ICardOwnershipStore
    {
        // Bulk ingest fields
        private SqliteConnection? _bulkConn;
        private SqliteTransaction? _bulkTx;
        private SqliteCommand? _bulkInsertCmd;
        private SqliteCommand? _bulkUpdateCmd;
        private SqliteCommand? _bulkCheckpointCmd;
        private bool _isBulkIngest = false;

        private readonly string _connectionString;

        public SQLiteCardOwnershipStore(string dbPath)
        {
            _connectionString = $"Data Source={dbPath}";
            EnsureTablesExist();
        }

        private void EnsureTablesExist()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS CardOwnership (
                CardHash TEXT PRIMARY KEY,
                OwnerPublicKey BLOB NOT NULL
            );
            CREATE TABLE IF NOT EXISTS OwnershipCheckpoint (
                Id INTEGER PRIMARY KEY CHECK(Id = 1),
                BlockIndex INTEGER NOT NULL
            );";
            cmd.ExecuteNonQuery();
        }

        public int GetCheckpoint()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT BlockIndex FROM OwnershipCheckpoint WHERE Id = 1;";
            var result = cmd.ExecuteScalar();
            return result == null || result is DBNull ? -1 : Convert.ToInt32(result);
        }

        public void SetCheckpoint(int blockIndex)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO OwnershipCheckpoint (Id, BlockIndex) VALUES (1, $blockIndex)
                ON CONFLICT(Id) DO UPDATE SET BlockIndex = $blockIndex;";
            cmd.Parameters.AddWithValue("$blockIndex", blockIndex);
            cmd.ExecuteNonQuery();
        }

        public byte[]? GetOwner(byte[] cardHash)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT OwnerPublicKey FROM CardOwnership WHERE CardHash = $cardHash;";
            cmd.Parameters.AddWithValue("$cardHash", cardHash.ToHex());
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return (byte[])reader["OwnerPublicKey"];
            }
            return null;
        }

        public void SetOwner(byte[] cardHash, byte[] ownerPublicKey)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO CardOwnership (CardHash, OwnerPublicKey) VALUES ($cardHash, $owner)
                ON CONFLICT(CardHash) DO UPDATE SET OwnerPublicKey = $owner;";
            cmd.Parameters.AddWithValue("$cardHash", cardHash.ToHex());
            cmd.Parameters.AddWithValue("$owner", ownerPublicKey);
            cmd.ExecuteNonQuery();
        }

        public void UpdateOwnershipForBlock(BCBlock block, int blockIndex)
        {
            // Fallback for non-bulk usage (single block update)
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var tx = conn.BeginTransaction();
            ApplyBlockToDb(conn, tx, block, blockIndex);
            tx.Commit();
        }

        // --- Bulk ingest methods ---
        /// <summary>
        /// Begins a bulk ingest session for efficient batch import of blocks.
        /// </summary>
        public void BeginBulkIngest()
        {
            if (_isBulkIngest) throw new InvalidOperationException("Bulk ingest already in progress.");
            _bulkConn = new SqliteConnection(_connectionString);
            _bulkConn.Open();
            _bulkTx = _bulkConn.BeginTransaction();
            _bulkInsertCmd = _bulkConn.CreateCommand();
            _bulkInsertCmd.CommandText = @"INSERT INTO CardOwnership (CardHash, OwnerPublicKey) VALUES ($cardHash, $owner)
                ON CONFLICT(CardHash) DO UPDATE SET OwnerPublicKey = $owner;";
            _bulkInsertCmd.Parameters.Add("$cardHash", SqliteType.Text);
            _bulkInsertCmd.Parameters.Add("$owner", SqliteType.Blob);
            _bulkInsertCmd.Transaction = _bulkTx;

            _bulkUpdateCmd = _bulkConn.CreateCommand();
            _bulkUpdateCmd.CommandText = @"UPDATE CardOwnership SET OwnerPublicKey = $owner WHERE CardHash = $cardHash;";
            _bulkUpdateCmd.Parameters.Add("$cardHash", SqliteType.Text);
            _bulkUpdateCmd.Parameters.Add("$owner", SqliteType.Blob);
            _bulkUpdateCmd.Transaction = _bulkTx;

            _bulkCheckpointCmd = _bulkConn.CreateCommand();
            _bulkCheckpointCmd.CommandText = @"INSERT INTO OwnershipCheckpoint (Id, BlockIndex) VALUES (1, $blockIndex)
                ON CONFLICT(Id) DO UPDATE SET BlockIndex = $blockIndex;";
            _bulkCheckpointCmd.Parameters.Add("$blockIndex", SqliteType.Integer);
            _bulkCheckpointCmd.Transaction = _bulkTx;

            _isBulkIngest = true;
        }

        /// <summary>
        /// Applies a block's ownership changes as part of a bulk ingest session.
        /// </summary>
        public void IngestBlock(BCBlock block, int blockIndex)
        {
            if (!_isBulkIngest || _bulkConn == null || _bulkTx == null)
                throw new InvalidOperationException("BeginBulkIngest() must be called before IngestBlock().");

            foreach (var transaction in block.Transactions)
            {
                switch (transaction)
                {
                    case MintCardTransaction mint:
                        _bulkInsertCmd.Parameters["$cardHash"].Value = mint.Card.ToHex();
                        _bulkInsertCmd.Parameters["$owner"].Value = mint.RecipientPublicKey;
                        _bulkInsertCmd.ExecuteNonQuery();
                        break;
                    case TradeCardsTransaction trade:
                        foreach (var card in trade.CardsFromUser1)
                        {
                            _bulkUpdateCmd.Parameters["$cardHash"].Value = card.ToHex();
                            _bulkUpdateCmd.Parameters["$owner"].Value = trade.User2PublicKey;
                            _bulkUpdateCmd.ExecuteNonQuery();
                        }
                        foreach (var card in trade.CardsFromUser2)
                        {
                            _bulkUpdateCmd.Parameters["$cardHash"].Value = card.ToHex();
                            _bulkUpdateCmd.Parameters["$owner"].Value = trade.User1PublicKey;
                            _bulkUpdateCmd.ExecuteNonQuery();
                        }
                        break;
                }
            }

            _bulkCheckpointCmd.Parameters["$blockIndex"].Value = blockIndex;
            _bulkCheckpointCmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Ends the bulk ingest session, committing all changes and closing resources.
        /// </summary>
        public void EndBulkIngest()
        {
            if (!_isBulkIngest || _bulkConn == null || _bulkTx == null)
                throw new InvalidOperationException("BeginBulkIngest() must be called before EndBulkIngest().");
            _bulkTx.Commit();
            _bulkInsertCmd?.Dispose();
            _bulkUpdateCmd?.Dispose();
            _bulkCheckpointCmd?.Dispose();
            _bulkTx.Dispose();
            _bulkConn.Close();
            _bulkConn.Dispose();
            _bulkInsertCmd = null;
            _bulkUpdateCmd = null;
            _bulkCheckpointCmd = null;
            _bulkTx = null;
            _bulkConn = null;
            _isBulkIngest = false;
        }

        // Helper for single-block update
        private void ApplyBlockToDb(SqliteConnection conn, SqliteTransaction tx, BCBlock block, int blockIndex)
        {
            foreach (var transaction in block.Transactions)
            {
                switch (transaction)
                {
                    case MintCardTransaction mint:
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = @"INSERT INTO CardOwnership (CardHash, OwnerPublicKey) VALUES ($cardHash, $owner)
                                ON CONFLICT(CardHash) DO UPDATE SET OwnerPublicKey = $owner;";
                            cmd.Parameters.AddWithValue("$cardHash", mint.Card.ToHex());
                            cmd.Parameters.AddWithValue("$owner", mint.RecipientPublicKey);
                            cmd.Transaction = tx;
                            cmd.ExecuteNonQuery();
                        }
                        break;
                    case TradeCardsTransaction trade:
                        foreach (var card in trade.CardsFromUser1)
                        {
                            using var cmd = conn.CreateCommand();
                            cmd.CommandText = @"UPDATE CardOwnership SET OwnerPublicKey = $owner WHERE CardHash = $cardHash;";
                            cmd.Parameters.AddWithValue("$cardHash", card.ToHex());
                            cmd.Parameters.AddWithValue("$owner", trade.User2PublicKey);
                            cmd.Transaction = tx;
                            cmd.ExecuteNonQuery();
                        }
                        foreach (var card in trade.CardsFromUser2)
                        {
                            using var cmd = conn.CreateCommand();
                            cmd.CommandText = @"UPDATE CardOwnership SET OwnerPublicKey = $owner WHERE CardHash = $cardHash;";
                            cmd.Parameters.AddWithValue("$cardHash", card.ToHex());
                            cmd.Parameters.AddWithValue("$owner", trade.User1PublicKey);
                            cmd.Transaction = tx;
                            cmd.ExecuteNonQuery();
                        }
                        break;
                }
            }
            using var checkpointCmd = conn.CreateCommand();
            checkpointCmd.CommandText = @"INSERT INTO OwnershipCheckpoint (Id, BlockIndex) VALUES (1, $blockIndex)
                ON CONFLICT(Id) DO UPDATE SET BlockIndex = $blockIndex;";
            checkpointCmd.Parameters.AddWithValue("$blockIndex", blockIndex);
            checkpointCmd.Transaction = tx;
            checkpointCmd.ExecuteNonQuery();
        }
    }
}
