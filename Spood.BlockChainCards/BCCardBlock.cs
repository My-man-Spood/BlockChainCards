using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Spood.BlockChainCards
{
    public class BCCardBlock
    {
        public byte[] PreviousHash { get; set; }
        public IEnumerable<BCTransaction> Transactions { get; set; }
        public byte[] BlockData { get; set; } = Array.Empty<byte>();
        public byte[] Hash { get; set; } = Array.Empty<byte>();

        public BCCardBlock(byte[] previous_hash, IEnumerable<BCTransaction> transactionList)
        {
            PreviousHash = previous_hash;
            Transactions = transactionList.ToList();
            // Convert PreviousHash to hex string
            string previousHashHex = BitConverter.ToString(PreviousHash).Replace("-", "");
            // Combine PreviousHash and Transactions into Data
            BlockData = PreviousHash
                .Concat(Transactions.SelectMany(t => t.ToBytes()))
                .ToArray(); 
            var sha = SHA256.Create();
            Hash = sha.ComputeHash(BlockData);
        }
    }
}
