using System;
using System.Collections.Concurrent;
using System.Linq;
using Bitcoin.Primitives.Fundamental;

namespace Network.Storage.Gossip
{
   public class GossipRepository : IGossipRepository
   {
      readonly ConcurrentDictionary<PublicKey, GossipNode> _nodes;
      readonly ConcurrentBag<PublicKey> _blacklistedNodeDictionary;
      readonly ConcurrentDictionary<ShortChannelId, GossipChannel> _channels;
      
      public GossipRepository()
      {
         _blacklistedNodeDictionary = new ConcurrentBag<PublicKey>();
         _nodes = new ConcurrentDictionary<PublicKey, GossipNode>();
         _channels = new ConcurrentDictionary<ShortChannelId, GossipChannel>();
      }

      public GossipNode AddNode(GossipNode node) => _nodes.AddOrUpdate(node.NodeId, node, 
         (key,  gossipNode) => node);

      public GossipNode? GetNode(PublicKey nodeId) =>
         _nodes.TryGetValue(nodeId, out GossipNode? node) ? node : default;

      public GossipNode[] GetNodes(params PublicKey[] keys) => _nodes.Values
         .Where(_ => keys.Contains(_.NodeId)).ToArray();

      public GossipChannel AddGossipChannel(GossipChannel channel) => _channels.AddOrUpdate(channel.ShortChannelId,
         channel, (id, gossipChannel) =>  channel);

      public GossipChannel? GetGossipChannel(ShortChannelId shortChannelId) =>
         _channels.TryGetValue(shortChannelId, out GossipChannel? channel) ? channel : default;

      public void RemoveGossipChannels(params ShortChannelId[] channelIds)
      {
         foreach (ShortChannelId shortChannelId in channelIds)
         {
            if (_channels.TryRemove(shortChannelId, out var channel))
               throw new InvalidOperationException();
         }
      }

      public bool IsNodeInBlacklistedList(PublicKey nodeId) => _blacklistedNodeDictionary.Contains(nodeId);

      public void AddNodeToBlacklist(params PublicKey[] publicKeys)
      {
         foreach (PublicKey publicKey in publicKeys)
         {
            if (_blacklistedNodeDictionary.Contains(publicKey))
               continue;
            _blacklistedNodeDictionary.Add(publicKey);      
         }
      }

      
   }
}