using System.Collections.Concurrent;
using Network.Protocol.Messages.Types;

namespace Network.Storage.Gossip
{
   public class GossipRepository : IGossipRepository
   {
      readonly ConcurrentDictionary<PublicKey, GossipNode> _dictionary;

      public GossipRepository() => _dictionary = new ConcurrentDictionary<PublicKey, GossipNode>();

      public GossipNode AddNode(GossipNode node) => _dictionary.AddOrUpdate(node.NodeId, node, 
         (key,  gossipNode) => node);

      public GossipNode? GetNode(PublicKey nodeId) =>
         _dictionary.TryGetValue(nodeId, out GossipNode? node) ? node : null;
   }
}