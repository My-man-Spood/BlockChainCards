namespace Spood.BlockChainCards.Lib.ByteUtils
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

        public static int ToInt32(this byte[] bytes)
        {
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        public static byte[] AsBytes(this int value)
        {
            return BitConverter.GetBytes(value);
        }
    }
}
