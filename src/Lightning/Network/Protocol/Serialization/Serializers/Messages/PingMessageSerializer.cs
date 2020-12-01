using System.Buffers;
using Network.Protocol.Messages;
using Network.Protocol.TlvStreams;

namespace Network.Protocol.Serialization.Serializers.Messages
{
   public class PingMessageSerializer : BaseMessageSerializer<PingMessage>
   {
      public PingMessageSerializer(ITlvStreamSerializer tlvStreamSerializer)
         : base(tlvStreamSerializer) { }

      public override void SerializeMessage(PingMessage message, int protocolVersion, NetworkPeerContext peerContext,
         IBufferWriter<byte> output)
      {
         output.WriteUShort(message.NumPongBytes, true);
         output.WriteUShort(message.BytesLen, true);
         output.WriteBytes(message.Ignored);
      }

      public override PingMessage DeserializeMessage(ref SequenceReader<byte> reader, int protocolVersion,
         NetworkPeerContext peerContext)
      {
         var numPongBytes = reader.ReadUShort(true);
         var bytesLen = reader.ReadUShort(true);

         return new PingMessage
         {
            NumPongBytes = numPongBytes,
            BytesLen = bytesLen,
            Ignored = reader.ReadBytes(bytesLen).ToArray()
         };
      }
   }
}