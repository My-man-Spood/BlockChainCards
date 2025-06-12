using System;
using System.IO;

namespace Spood.BlockChainCards.Lib.Configuration
{
    /// <summary>
    /// Manages all file and directory paths for the application
    /// </summary>
    public class PathConfiguration
    {
        private readonly string basePath;

        public PathConfiguration(string basePath)
        {
            // Convert to absolute path if relative
            this.basePath = Path.GetFullPath(basePath);
            
            // Ensure the base directory exists
            Directory.CreateDirectory(this.basePath);
        }

        public int BlockSafetyThreshold { get; set; } = 5;

        // Base directories
        public string BasePath => basePath;
        public string BlockchainPath => Path.Combine(basePath, "Blockchain");
        public string BlockFileIndexPath => BlockchainPath; // Same path as blockchain for index files
        
        // Files
        public string CardsJsonPath => Path.Combine(basePath, "cards.json");
        public string CardOwnershipDbPath => Path.Combine(basePath, "card-ownership-db.sqlite");
        public string AuthorityWalletPath => Path.Combine(basePath, "Authority-wallet.json");
        public string BlockIndexFile => Path.Combine(basePath, "block-index.sqlite");
        
        // Ensures all required directories exist
        public void EnsureDirectoriesExist()
        {
            Directory.CreateDirectory(BasePath);
            Directory.CreateDirectory(BlockchainPath);
        }
        
        // Get block file path by name
        public string GetBlockFilePath(string fileName)
        {
            return Path.Combine(BlockchainPath, fileName);
        }
        
        // Get next block file path (util method)
        public string GetNextBlockFilePath(string currentBlockFileName)
        {
            // Extract just the filename if a full path was provided
            string fileName = Path.GetFileName(currentBlockFileName);
            
            // Parse the number from the filename
            var withoutPrefix = fileName.Replace("_", "");
            var fileNumberWithExt = withoutPrefix.Split('.');
            var number = int.Parse(fileNumberWithExt[0]);
            
            // Generate new filename with next number
            return $"_{(number + 1):D6}.blk";
        }
    }
}
