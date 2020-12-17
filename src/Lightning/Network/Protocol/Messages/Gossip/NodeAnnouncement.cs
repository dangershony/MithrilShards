using MithrilShards.Core.Network.Protocol.Serialization;
using Network.Protocol.Messages.Types;

namespace Network.Protocol.Messages.Gossip
{
   [NetworkMessage(COMMAND)]
   public class NodeAnnouncement : BaseMessage
   {
      private const string COMMAND = "257";

      public NodeAnnouncement()
      {
         Signature = new Signature();
         Len = 0;
         Features = new byte[0];
         Timestamp = 0;
         NodeId = new Point();
         RgbColor = new byte[0];
         Alias = new byte[0];
         Addrlen = 0;
         Addresses = new byte[0];
      }

      public override string Command => COMMAND;

      public Signature Signature { get; set; }

      public ushort Len { get; set; }

      public byte[] Features { get; set; }

      public uint Timestamp { get; set; }

      public Point NodeId { get; set; }

      public byte[] RgbColor { get; set; }

      public byte[] Alias { get; set; }

      public ushort Addrlen { get; set; }

      public byte[] Addresses { get; set; }
   }
}