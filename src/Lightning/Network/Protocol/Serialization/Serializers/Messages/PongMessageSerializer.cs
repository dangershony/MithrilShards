using System.Buffers;
using Network.Protocol.Messages;
using Network.Protocol.TlvStreams;

namespace Network.Protocol.Serialization.Serializers.Messages
{
   public class PongMessageSerializer : BaseMessageSerializer<PongMessage>
   {
      public PongMessageSerializer(ITlvStreamSerializer tlvStreamSerializer)
         : base(tlvStreamSerializer)
      { }

      public override void SerializeMessage(PongMessage message, int protocolVersion, NetworkPeerContext peerContext,
         IBufferWriter<byte> output)
      {
         output.WriteUShort(message.BytesLen, true);
         output.WriteBytes(message.Ignored);
      }

      public override PongMessage DeserializeMessage(ref SequenceReader<byte> reader, int protocolVersion,
         NetworkPeerContext peerContext)
      {
         ushort bytesLen = reader.ReadUShort(true);

         return new PongMessage
         {
            BytesLen = bytesLen,
            Ignored = reader.ReadBytes(bytesLen).ToArray()
         };
      }
   }
}