using System;
using System.IO;
using Xunit;
using Spood.BlockChainCards.Lib.Configuration;
using Spood.BlockChainCards.Commands;
using Spood.BlockChainCards;
using Spood.BlockChainCards.Serialization;
using System.Text.Json;

namespace Spood.BlockChainCards.Testing.IntegrationTests;

public class InitializationCommandTest
{
    [Fact]
    public void InitializationCommand_CreatesFoldersAndGenesisBlock()
    {
        // Arrange
        var tempRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempRoot);
        var pathConfig = new PathConfiguration(tempRoot);
        // The constructor is: FileBlockChainReader(string blockchainPath, JsonSerializerOptions serializerOptions, IWalletReader walletReader, ICardOwnershipStore cardOwnershipStore, PathConfiguration pathConfig)
        var fileBlockChainReader = new FileBlockChainReader(
            new BlockFileReader(pathConfig.BlockchainPath),
            new BlockFileIndex(pathConfig),
            new JsonWalletReader(new JsonSerializerOptions()), // IWalletReader
            new SQLiteCardOwnershipStore(pathConfig.CardOwnershipDbPath), // ICardOwnershipStore
            pathConfig
        );
        var command = new InitializationCommand(pathConfig, fileBlockChainReader);

        try
        {
            // Act
            command.Execute(Array.Empty<string>());

            // Assert directories
            Assert.True(Directory.Exists(pathConfig.BlockchainPath));
            Assert.True(Directory.Exists(Path.Combine(pathConfig.BasePath, "cards")));

            // Assert genesis block file
            var genesisBlockFile = Path.Combine(pathConfig.BlockchainPath, "_000000.blk");
            Assert.True(File.Exists(genesisBlockFile));

            // Assert genesis block file header (should be 4 bytes for block count)
            var headerBytes = File.ReadAllBytes(genesisBlockFile);
            Assert.True(headerBytes.Length > 4, "Genesis block file should contain more than just the header");
            Assert.Equal(BitConverter.GetBytes(1), headerBytes[..4]);

            Assert.True(File.Exists(pathConfig.CardOwnershipDbPath));
            Assert.True(File.Exists(pathConfig.BlockFileIndexPath));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }
}
