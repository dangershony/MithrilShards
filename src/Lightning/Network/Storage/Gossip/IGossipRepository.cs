using Bitcoin.Primitives.Fundamental;

namespace Network.Storage.Gossip
{
   public interface IGossipRepository
   {
      GossipNode AddNode(GossipNode node);

      GossipNode? GetNode(PublicKey nodeId);

      bool IsNodeInBlacklistedList(PublicKey nodeId);
   }
}