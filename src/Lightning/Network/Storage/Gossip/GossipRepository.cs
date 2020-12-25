using System.Collections.Concurrent;
using Bitcoin.Primitives.Fundamental;
using Network.Protocol.Messages.Types;

namespace Network.Storage.Gossip
{
   public class GossipRepository : IGossipRepository
   {
      readonly ConcurrentDictionary<PublicKey, GossipNode> _dictionary;
      readonly ConcurrentDictionary<PublicKey, GossipNode> _blacklistedNodeDictionary;
      
      public GossipRepository()
      {
         _blacklistedNodeDictionary = new ConcurrentDictionary<PublicKey, GossipNode>();
         _dictionary = new ConcurrentDictionary<PublicKey, GossipNode>();
      }

      public GossipNode AddNode(GossipNode node) => _dictionary.AddOrUpdate(node.NodeId, node, 
         (key,  gossipNode) => node);

      public GossipNode? GetNode(PublicKey nodeId) =>
         _dictionary.TryGetValue(nodeId, out GossipNode? node) ? node : null;

      public bool IsNodeInBlacklistedList(PublicKey nodeId) => _dictionary.ContainsKey(nodeId);
   }
}