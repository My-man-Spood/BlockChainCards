namespace Spood.BlockChainCards.Serialization;

using Spood.BlockChainCards.Lib;

/// <summary>
/// Represents the result of reading a block from a block file.
/// </summary>
public record BlockReadResult(BCBlock Block, int Length, int DataOffset);
