namespace Spood.BlockChainCards.Lib.Utils
{
    public static class ByteArrayExtensions
    {
        public static string ToHex(this byte[] bytes)
        {
            return Convert.ToHexString(bytes);
        }
    }
}
