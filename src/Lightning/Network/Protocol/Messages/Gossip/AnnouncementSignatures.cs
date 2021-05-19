using Bitcoin.Primitives.Fundamental;
using MithrilShards.Core.Network.Protocol.Serialization;
using Network.Protocol.Messages.Types;

namespace Network.Protocol.Messages.Gossip
{
   [NetworkMessage(COMMAND)]
   public class AnnouncementSignatures : BaseMessage
   {
      private const string COMMAND = "259";

      public AnnouncementSignatures(ChannelId channelId, ShortChannelId shortChannelId, CompressedSignature nodeSignature, CompressedSignature bitcoinSignature)
      {
         ChannelId = channelId;
         ShortChannelId = shortChannelId;
         NodeSignature = nodeSignature;
         BitcoinSignature = bitcoinSignature;
      }

      public AnnouncementSignatures()
      {
         ChannelId = new ChannelId(new byte[] {0});
         ShortChannelId = new ShortChannelId(new byte[] {0});
         NodeSignature = new CompressedSignature(new byte[] {0});
         BitcoinSignature = new CompressedSignature(new byte[] {0});
      }

      public override string Command => COMMAND;

      public ChannelId ChannelId { get; set; }

      public ShortChannelId ShortChannelId { get; set; }

      public CompressedSignature NodeSignature { get; set; }

      public CompressedSignature BitcoinSignature { get; set; }
   }
}