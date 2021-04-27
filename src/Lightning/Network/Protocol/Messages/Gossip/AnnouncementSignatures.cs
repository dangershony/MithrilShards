using Bitcoin.Primitives.Fundamental;
using MithrilShards.Core.Network.Protocol.Serialization;
using Network.Protocol.Messages.Types;

namespace Network.Protocol.Messages.Gossip
{
   [NetworkMessage(COMMAND)]
   public class AnnouncementSignatures : BaseMessage
   {
      private const string COMMAND = "256";

      public override string Command => COMMAND;

      public ChannelId ChannelId { get; set; }

      public ShortChannelId ShortChannelId { get; set; }

      public CompressedSignature NodeSignature { get; set; }

      public CompressedSignature BitcoinSignature { get; set; }
   }
}