using System.Buffers;
using Network.Protocol.Messages.Gossip;
using Network.Protocol.Messages.Types;
using Network.Protocol.TlvStreams;

namespace Network.Protocol.Serialization.Serializers.Messages.Gossip
{
   public class NodeAnnouncementSerializer : BaseMessageSerializer<NodeAnnouncement>
   {
      public NodeAnnouncementSerializer(ITlvStreamSerializer tlvStreamSerializer) 
         : base(tlvStreamSerializer)
      { }

      public override void SerializeMessage(NodeAnnouncement message, int protocolVersion,
         NetworkPeerContext peerContext,
         IBufferWriter<byte> output)
      {
         output.WriteBytes(message.Signature);
         output.WriteUShort(message.Len, true);
         output.WriteBytes(message.Features);
         output.WriteUInt(message.Timestamp, true);
         output.WriteBytes(message.NodeId);
         output.WriteBytes(message.RgbColor);
         output.WriteBytes(message.Alias);
         output.WriteUShort(message.Addrlen, true);
         output.WriteBytes(message.Addresses);
      }

      public override NodeAnnouncement DeserializeMessage(ref SequenceReader<byte> reader, int protocolVersion,
         NetworkPeerContext peerContext)
      {
         var message = new NodeAnnouncement
         {
            Signature = (Signature)reader.ReadBytes(Signature.SIGNATURE_LENGTH), 
            Len = reader.ReadUShort(true)
         };

         message.Features = reader.ReadBytes(message.Len).ToArray();
         message.Timestamp = reader.ReadUInt(true);
         message.NodeId = (Point) reader.ReadBytes(Point.POINT_LENGTH);
         message.RgbColor = reader.ReadBytes(3).ToArray();
         message.Alias = reader.ReadBytes(32).ToArray();
         message.Addrlen = reader.ReadUShort(true);
         message.Addresses = reader.ReadBytes(message.Addrlen).ToArray();
         
         return message;
      }
   }
}