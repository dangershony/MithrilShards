using MithrilShards.Core.EventBus;
using Network.Protocol.Messages.Types;

namespace Network.Storage.Gossip
{
   public class GossipNode : EventBase
   {
      public PublicKey NodeId { get; set; }
      
      public byte[] Features { get; set; }

      public uint Timestamp { get; set; }
      
      public byte[] RgbColor { get; set; }

      public byte[] Alias { get; set; }

      public ushort Addrlen { get; set; }

      public byte[] Addresses { get; set; }
   }
}