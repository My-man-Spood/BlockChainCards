namespace Spood.BlockChainCards.Serialization;

public record BlockMetaData(byte[] BlockHash, string BlockFilePath, int BlockIndexGlobal, int BlockOffset, int BlockSize);
