using Network.Protocol.Messages.Types;

namespace Network.Storage.Gossip
{
   public class GossipNodeTimestampFilter
   {
      public GossipNodeTimestampFilter(ChainHash chainHash)
      {
         ChainHash = chainHash;
      }

      public ChainHash ChainHash { get; set; }
      
      public uint FirstTimestamp { get; set; }

      public uint TimestampRange { get; set; }
   }
}