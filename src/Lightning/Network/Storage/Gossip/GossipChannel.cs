using Bitcoin.Primitives.Fundamental;
using Network.Protocol.Messages.Types;

namespace Network.Storage.Gossip
{
   public class GossipChannel
   {

      public GossipChannel(byte[] features, ChainHash chainHash, ShortChannelId shortChannelId, PublicKey nodeId1, PublicKey nodeId2, PublicKey bitcoinKey1, PublicKey bitcoinKey2)
      {
         Features = features;
         ChainHash = chainHash;
         ShortChannelId = shortChannelId;
         NodeId1 = nodeId1;
         NodeId2 = nodeId2;
         BitcoinKey1 = bitcoinKey1;
         BitcoinKey2 = bitcoinKey2;
      }

      public byte[] Features { get; set; }

      public ChainHash ChainHash { get; set; }

      public ShortChannelId ShortChannelId { get; set; }

      public PublicKey NodeId1 { get; set; }
      
      public PublicKey NodeId2 { get; set; }
      
      public PublicKey BitcoinKey1 { get; set; }
      
      public PublicKey BitcoinKey2 { get; set; }
      
      
   }
}