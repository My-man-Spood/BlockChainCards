using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spood.BlockChainCards
{
    public class BCTransaction
    {
        public Guid Sender { get; set; }
        public Guid Receiver { get; set; }
        public IEnumerable<Guid> CardsSent { get; set; }
        public BCTransaction(Guid Sender, Guid Receiver, IEnumerable<Guid> CardsReceived) 
        {
            this.Sender = Sender;
            this.Receiver = Receiver;
            this.CardsSent = CardsReceived.ToList();
        }

        public byte[] ToBytes()
        {
            var senderBytes = Sender.ToByteArray();
            var receiverBytes = Receiver.ToByteArray();
            var cardsBytes = CardsSent.Select(card => card.ToByteArray()).SelectMany(bytes => bytes).ToArray();
            return senderBytes
                .Concat(receiverBytes)
                .Concat(cardsBytes)
                .ToArray();
        }
    }
}
