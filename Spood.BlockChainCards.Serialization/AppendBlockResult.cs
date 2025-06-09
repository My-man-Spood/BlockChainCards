namespace Spood.BlockChainCards.Serialization;

public record AppendBlockResult(byte[] BlockHash, string BlockFilePath, int BlockIndexGlobal, int BlockOffset, int BlockSize);
