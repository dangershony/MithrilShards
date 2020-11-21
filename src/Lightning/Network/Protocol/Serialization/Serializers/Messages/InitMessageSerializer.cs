using System.Buffers;
using Network.Protocol.Messages;

namespace Network.Protocol.Serialization.Serializers.Messages
{
   public class InitMessageSerializer : NetworkMessageSerializerBase<InitMessage>
   {
      public override void Serialize(InitMessage message, int protocolVersion, NetworkPeerContext peerContext, IBufferWriter<byte> output)
      {
         output.WriteUShort((ushort)message.GlobalFeatures.Length);
         output.Write(message.GlobalFeatures);

         output.WriteUShort((ushort)message.Features.Length);
         output.Write(message.Features);

         // todo: write tlv_stream
      }

      public override InitMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion, NetworkPeerContext peerContext)
      {
         var message = new InitMessage();

         ushort len = reader.ReadUShort();
         message.GlobalFeatures = reader.ReadBytes(len).ToArray();

         len = reader.ReadUShort();
         message.Features = reader.ReadBytes(len).ToArray();

         // todo: read tlv_stream

         return message;
      }
   }
}