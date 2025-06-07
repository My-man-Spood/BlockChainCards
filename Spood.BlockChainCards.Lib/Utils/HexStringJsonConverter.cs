using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spood.BlockChainCards.Lib.Utils
{
    public class HexStringJsonConverter : JsonConverter<byte[]>
    {
        public override byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var hex = reader.GetString();
            if (string.IsNullOrEmpty(hex)) return Array.Empty<byte>();
            return Convert.FromHexString(hex);
        }

        public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToHex());
        }
    }
}
