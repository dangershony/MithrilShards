using System.Buffers;
using Network.Protocol.TlvStreams;
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
         output.WriteBytes(message.ChannelId);
         output.WriteUShort(message.Len,true);
         output.WriteBytes(message.Data);
      }

      public override ErrorMessage DeserializeMessage(ref SequenceReader<byte> reader, int protocolVersion,
         NetworkPeerContext peerContext)
      {
         byte[]? channelId = reader.ReadBytes(32).ToArray();
         ushort len = reader.ReadUShort(true);

         return new ErrorMessage
         {
            ChannelId = channelId, Len = len, Data = reader.ReadBytes(len).ToArray()
         };
      }
   }
}