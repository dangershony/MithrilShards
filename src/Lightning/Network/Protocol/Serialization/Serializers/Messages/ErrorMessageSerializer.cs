using System.Buffers;
using System.Linq;
using Network.Protocol.Messages;

namespace Network.Protocol.Serialization.Serializers.Messages
{
   public class ErrorMessageSerializer : BaseMessageSerializer<ErrorMessage>
   {
      public ErrorMessageSerializer(ITlvStreamSerializer tlvStreamSerializer) 
         : base(tlvStreamSerializer)
      { }

      public override void SerializeMessage(ErrorMessage message, int protocolVersion, NetworkPeerContext peerContext,
         IBufferWriter<byte> output)
      {
         output.WriteByteArray(message.ChannelId);
         output.WriteUShort(message.Len);
         output.WriteByteArray(message.Data);
      }

      public override ErrorMessage DeserializeMessage(ref SequenceReader<byte> reader, int protocolVersion,
         NetworkPeerContext peerContext)
      {
         var channelId = reader.ReadBytes(32).ToArray();
         ushort len = reader.ReadUShort();

         return new ErrorMessage
         {
            ChannelId = channelId, Len = len, Data = reader.ReadBytes(len).ToArray()
         };
      }
   }
}