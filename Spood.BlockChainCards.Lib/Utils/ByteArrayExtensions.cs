namespace Spood.BlockChainCards.Lib.Utils
{
    public static class ByteArrayExtensions
    {
        public static string ToHex(this byte[] bytes)
        {
            return Convert.ToHexString(bytes);
        }

        /// <summary>
        /// Returns the length of the array as a little-endian byte array
        /// </summary>
        public static byte[] LengthBytesLE(this byte[] data)
        {
            var lengthBytes = BitConverter.GetBytes(data.Length);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(lengthBytes);
            return lengthBytes;
        }
    }
}
