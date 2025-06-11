using Microsoft.Data.Sqlite;
using Spood.BlockChainCards.Lib.Configuration;

namespace Spood.BlockChainCards.Serialization
{
    /// <summary>
    /// Provides a persistent SQLite-backed index for block lookup by hash, height, or file location.
    /// </summary>
    public class BlockFileIndex
    {
        private readonly string _dbPath;
        private SqliteConnection? _bulkConn;
        private SqliteTransaction? _bulkTxn;
        private bool _schemaEnsured;
        private SqliteCommand? _bulkAddBlockCmd;
        private readonly PathConfiguration? _pathConfig;

        // Constructor with explicit path for backward compatibility
        public BlockFileIndex(string directory)
        {
            _dbPath = Path.Combine(directory, "block_index.sqlite");
            _pathConfig = null; // Not using PathConfiguration
            EnsureSchema();
        }
        
        // Constructor with PathConfiguration for better path management
        public BlockFileIndex(PathConfiguration pathConfig)
        {
            _pathConfig = pathConfig;
            _dbPath = pathConfig.BlockIndexFile; // Uses the path from configuration
            EnsureSchema();
        }

        // Ensures schema exists (run on every connection open)
        private void EnsureSchema(SqliteConnection? conn = null)
        {
            if (_schemaEnsured && conn == null) return;
            SqliteConnection useConn = conn ?? new SqliteConnection($"Data Source={_dbPath}");
            bool opened = false;
            if (useConn.State != System.Data.ConnectionState.Open) { useConn.Open(); opened = true; }
            using var cmd = useConn.CreateCommand();
            cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS blocks (
                hash BLOB PRIMARY KEY,
                height INTEGER,
                file TEXT,
                offset INTEGER,
                length INTEGER
            );
            CREATE INDEX IF NOT EXISTS idx_blocks_height ON blocks(height);
            ";
            cmd.ExecuteNonQuery();
            if (opened) useConn.Dispose();
            if (conn == null) _schemaEnsured = true;
        }

        public void BeginBulkIngest()
        {
            if (_bulkConn != null) throw new InvalidOperationException("Bulk ingest already started");
            _bulkConn = new SqliteConnection($"Data Source={_dbPath}");
            _bulkConn.Open();
            EnsureSchema(_bulkConn);
            _bulkTxn = _bulkConn.BeginTransaction();
            _bulkAddBlockCmd = _bulkConn.CreateCommand();
            _bulkAddBlockCmd.Transaction = _bulkTxn;
            _bulkAddBlockCmd.CommandText = "INSERT OR REPLACE INTO blocks (hash, height, file, offset, length) VALUES (@hash, @height, @file, @offset, @length);";
            _bulkAddBlockCmd.Parameters.Add("@hash", SqliteType.Blob, 32);
            _bulkAddBlockCmd.Parameters.Add("@height", SqliteType.Integer);
            _bulkAddBlockCmd.Parameters.Add("@file", SqliteType.Text);
            _bulkAddBlockCmd.Parameters.Add("@offset", SqliteType.Integer);
            _bulkAddBlockCmd.Parameters.Add("@length", SqliteType.Integer);
        }

        public void EndBulkIngest()
        {
            if (_bulkConn == null) throw new InvalidOperationException("Bulk ingest not started");
            _bulkAddBlockCmd?.Dispose();
            _bulkAddBlockCmd = null;
            _bulkTxn?.Commit();
            _bulkTxn?.Dispose();
            _bulkConn.Dispose();
            _bulkTxn = null;
            _bulkConn = null;
        }

        /// <summary>
        /// Adds a block to the index using a short-lived connection (normal mode).
        /// </summary>
        public void AddBlock(byte[] hash, int height, string file, int offset, int length)
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath};");
            conn.Open();
            EnsureSchema(conn);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT OR REPLACE INTO blocks (hash, height, file, offset, length) VALUES (@hash, @height, @file, @offset, @length);";
            cmd.Parameters.Add("@hash", SqliteType.Blob, 32).Value = hash;
            cmd.Parameters.Add("@height", SqliteType.Integer).Value = height;
            cmd.Parameters.Add("@file", SqliteType.Text).Value = file;
            cmd.Parameters.Add("@offset", SqliteType.Integer).Value = offset;
            cmd.Parameters.Add("@length", SqliteType.Integer).Value = length;
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Adds a block to the index during bulk ingest. Only valid if BeginBulkIngest has been called.
        /// </summary>
        public void IngestBlock(byte[] hash, int height, string file, int offset, int length)
        {
            if (_bulkConn == null || _bulkAddBlockCmd == null)
                throw new InvalidOperationException("Bulk ingest not started. Call BeginBulkIngest first.");
            _bulkAddBlockCmd.Parameters["@hash"].Value = hash;
            _bulkAddBlockCmd.Parameters["@height"].Value = height;
            _bulkAddBlockCmd.Parameters["@file"].Value = file;
            _bulkAddBlockCmd.Parameters["@offset"].Value = offset;
            _bulkAddBlockCmd.Parameters["@length"].Value = length;
            _bulkAddBlockCmd.ExecuteNonQuery();
        }

        public BlockIndexMetaData? LookupByHash(byte[] hash)
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath};");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT file, height, offset, length FROM blocks WHERE hash = @hash;";
            cmd.Parameters.Add("@hash", SqliteType.Blob, 32).Value = hash;
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new BlockIndexMetaData(
                    hash,
                    reader.GetString(0),
                    reader.GetInt32(1),
                    reader.GetInt32(2),
                    reader.GetInt32(3)
                );
            }
            return null;
        }

        public BlockIndexMetaData LookupByHeight(int height)
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath};");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT file, height, offset, length, hash FROM blocks WHERE height = @height;";
            cmd.Parameters.Add("@height", SqliteType.Integer).Value = height;
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var hash = new byte[32];
                reader.GetBytes(3, 0, hash, 0, 32);
                return new BlockIndexMetaData(
                    hash,
                    reader.GetString(0),
                    reader.GetInt32(1),
                    reader.GetInt32(2),
                    reader.GetInt32(3)
                );
            }
            return null;
        }

        public int GetTotalBlockCount()
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath};");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM blocks;";
            return Convert.ToInt32((long)cmd.ExecuteScalar());
        }

        public void Initialize()
        {
            // Ensure the index file and schema exist (idempotent)
            if (!File.Exists(_dbPath))
            {
                File.Create(_dbPath).Dispose();
            }
            using var conn = new SqliteConnection($"Data Source={_dbPath};");
            conn.Open();
            EnsureSchema(conn);
        }
    }
}
