using System.Buffers;
using Network.Protocol.Messages;

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
         output.WriteUShort(message.BytesLen);
         output.WriteBytes(message.Ignored);
      }

      public override PongMessage DeserializeMessage(ref SequenceReader<byte> reader, int protocolVersion,
         NetworkPeerContext peerContext)
      {
         var bytesLen = reader.ReadUShort();
         
         return new PongMessage
         {
            BytesLen = bytesLen,
            Ignored = reader.ReadBytes(bytesLen).ToArray()
         };
      }
   }
}