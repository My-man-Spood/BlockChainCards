using Spood.BlockChainCards.Lib;
using Spood.BlockChainCards.Lib.Transactions;
using Spood.BlockChainCards.Serialization;

namespace Spood.BlockChainCards.Testing.IntegrationTests
{
    public class BlockFileStreamHandlerTest
    {
        [Fact]
        public void BlockFile_RoundTrip_Serialization_Works()
        {
            // Arrange
            var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                // Create a dummy block
                var blockToWrite = new BCBlock
                {
                    PreviousHash = new byte[32], // All zeros
                    Transactions = new List<BCTransaction>(),
                    Timestamp = DateTime.UtcNow,
                };

                // Write the block count header (1 block)
                File.WriteAllBytes(tempFile, BitConverter.GetBytes(1));

                // Append the serialized block
                using (var handler = new BlockFileStreamHandler(tempFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    var serializedBlock = BlockSerializer.Serialize(blockToWrite);
                    handler.AppendSerializedBlock(serializedBlock);
                }

                // Read the block back
                BCBlock blockRead;
                using (var handler = new BlockFileStreamHandler(tempFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    handler.PositionAtFirstBlock();
                    var readResult = handler.ReadBlockAtCurrentPosition();
                    blockRead = readResult.Block;
                }

                // Assert field-by-field
                Assert.Equal(blockToWrite.PreviousHash, blockRead.PreviousHash);
                Assert.Equal(blockToWrite.Transactions.Count, blockRead.Transactions.Count);
                Assert.Equal(blockToWrite.Timestamp, blockRead.Timestamp, precision: TimeSpan.FromSeconds(1));
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }
    }
}