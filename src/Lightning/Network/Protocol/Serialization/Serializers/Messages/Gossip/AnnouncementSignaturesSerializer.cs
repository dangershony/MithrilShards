using System.Buffers;
using Bitcoin.Primitives.Fundamental;
using Network.Protocol.Messages.Gossip;
using Network.Protocol.Messages.Types;
using Network.Protocol.TlvStreams;

namespace Network.Protocol.Serialization.Serializers.Messages.Gossip
{
   public class AnnouncementSignaturesSerializer : BaseMessageSerializer<AnnouncementSignatures>
   {
      public AnnouncementSignaturesSerializer(ITlvStreamSerializer tlvStreamSerializer) : base(tlvStreamSerializer)
      {
      }

      public override void SerializeMessage(AnnouncementSignatures message, int protocolVersion,
         NetworkPeerContext peerContext,
         IBufferWriter<byte> output)
      {
         output.WriteBytes(message.ChannelId);
         output.WriteBytes(message.ShortChannelId);
         output.WriteBytes(message.NodeSignature);
         output.WriteBytes(message.BitcoinSignature);
      }

      public override AnnouncementSignatures DeserializeMessage(ref SequenceReader<byte> reader, int protocolVersion,
         NetworkPeerContext peerContext)
      {
         return new AnnouncementSignatures
         {
            ChannelId = (ChannelId) reader.ReadBytes(32).ToArray(),
            ShortChannelId = (ShortChannelId) reader.ReadBytes(8).ToArray(),
            NodeSignature = (CompressedSignature) reader.ReadBytes(CompressedSignature.SIGNATURE_LENGTH).ToArray(),
            BitcoinSignature = (CompressedSignature) reader.ReadBytes(CompressedSignature.SIGNATURE_LENGTH).ToArray(),
         };
      }
   }
}