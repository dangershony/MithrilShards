using System.Buffers;
using Bitcoin.Primitives.Fundamental;
using Network.Protocol.Messages.Gossip;
using Network.Protocol.Messages.Types;
using Network.Protocol.TlvStreams;
using CompressedSignature = Network.Protocol.Messages.Types.CompressedSignature;
using PublicKey = Bitcoin.Primitives.Fundamental.PublicKey;

namespace Network.Protocol.Serialization.Serializers.Messages.Gossip
{
   public class ChannelAnnouncementSerializer : BaseMessageSerializer<ChannelAnnouncement>
   {
      public ChannelAnnouncementSerializer(ITlvStreamSerializer tlvStreamSerializer) : base(tlvStreamSerializer)
      {
      }

      public override void SerializeMessage(ChannelAnnouncement message, int protocolVersion,
         NetworkPeerContext peerContext,
         IBufferWriter<byte> output)
      {
         output.WriteBytes(message.NodeSignature1);
         output.WriteBytes(message.NodeSignature2);
         output.WriteBytes(message.BitcoinSignature1);
         output.WriteBytes(message.BitcoinSignature2);
         output.WriteUShort(message.Len);
         output.WriteBytes(message.Features);
         output.WriteBytes(message.ChainHash);
         output.WriteBytes(message.ShortChannelId);
         output.WriteBytes(message.NodeId1);
         output.WriteBytes(message.NodeId2);
         output.WriteBytes(message.BitcoinKey1);
         output.WriteBytes(message.BitcoinKey2);
      }

      public override ChannelAnnouncement DeserializeMessage(ref SequenceReader<byte> reader, int protocolVersion,
         NetworkPeerContext peerContext)
      {
         var message = new ChannelAnnouncement
         {
            NodeSignature1 = (CompressedSignature)reader.ReadBytes(CompressedSignature.SIGNATURE_LENGTH),
            NodeSignature2 = (CompressedSignature)reader.ReadBytes(CompressedSignature.SIGNATURE_LENGTH),
            BitcoinSignature1 = (CompressedSignature)reader.ReadBytes(CompressedSignature.SIGNATURE_LENGTH),
            BitcoinSignature2 = (CompressedSignature)reader.ReadBytes(CompressedSignature.SIGNATURE_LENGTH),
         };

         message.Len = reader.ReadUShort();
         message.Features = reader.ReadBytes(message.Len).ToArray();
         message.ChainHash = (ChainHash) reader.ReadBytes(32);
         message.ShortChannelId = (ShortChannelId)reader.ReadBytes(8).ToArray();
         message.NodeId1 = (PublicKey)reader.ReadBytes(PublicKey.LENGTH);
         message.NodeId2 = (PublicKey)reader.ReadBytes(PublicKey.LENGTH);
         message.BitcoinKey1 = (PublicKey)reader.ReadBytes(PublicKey.LENGTH);
         message.BitcoinKey2 = (PublicKey)reader.ReadBytes(PublicKey.LENGTH);
         
         return message;
      }
   }
}