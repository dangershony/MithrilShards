using Network.Protocol.Messages.Types;

namespace Network.Storage.Gossip
{
   public interface IGossipRepository
   {
      GossipNode AddNode(GossipNode node);

      GossipNode? GetNode(PublicKey nodeId);
   }
}