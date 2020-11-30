using System.Buffers;
using Network.Protocol.Messages;

namespace Network.Protocol.Serialization.Serializers.Messages
{
   public class PingMessageSerializer : BaseMessageSerializer<PingMessage>
   {
      public PingMessageSerializer(ITlvStreamSerializer tlvStreamSerializer) 
         : base(tlvStreamSerializer) { }

      public override void SerializeMessage(PingMessage message, int protocolVersion, NetworkPeerContext peerContext,
         IBufferWriter<byte> output)
      {
         output.WriteUShort(message.NumPongBytes);
         output.WriteUShort(message.BytesLen);
         output.WriteBytes(message.Ignored);
      }

      public override PingMessage DeserializeMessage(ref SequenceReader<byte> reader, int protocolVersion,
         NetworkPeerContext peerContext)
      {
         var numPongBytes = reader.ReadUShort();
         var bytesLen = reader.ReadUShort();
         
         return new PingMessage
         {
            NumPongBytes = numPongBytes,
            BytesLen = bytesLen,
            Ignored = reader.ReadBytes(bytesLen).ToArray()
         };
      }
   }
}