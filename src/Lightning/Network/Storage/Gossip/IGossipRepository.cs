using Bitcoin.Primitives.Fundamental;

namespace Network.Storage.Gossip
{
   public interface IGossipRepository
   {
      GossipNode AddNode(GossipNode node);

      GossipNode? GetNode(PublicKey nodeId);
      GossipNode[] GetNodes(params PublicKey[] keys);
      
      GossipChannel AddGossipChannel(GossipChannel channel);

      GossipChannel? GetGossipChannel(ShortChannelId shortChannelId);

      void RemoveGossipChannels(params ShortChannelId[] channelIds);
      
      bool IsNodeInBlacklistedList(PublicKey nodeId);

      void AddNodeToBlacklist(params PublicKey[] publicKeys);
   }
}