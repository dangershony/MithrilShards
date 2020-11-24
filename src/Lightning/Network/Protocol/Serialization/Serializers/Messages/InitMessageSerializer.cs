using System.Buffers;
using Network.Protocol.Messages;

namespace Network.Protocol.Serialization.Serializers.Messages
{
   public class InitMessageSerializer : BaseMessageSerializer<InitMessage>
   {
      public InitMessageSerializer(ITlvStreamSerializer tlvStreamSerializer) : base(tlvStreamSerializer)
      {
      }

      public override void SerializeMessage(InitMessage message, int protocolVersion, NetworkPeerContext peerContext, IBufferWriter<byte> output)
      {
         output.WriteUShort((ushort)message.GlobalFeatures.Length);
         output.Write(message.GlobalFeatures);

         output.WriteUShort((ushort)message.Features.Length);
         output.Write(message.Features);
      }

      public override InitMessage DeserializeMessage(ref SequenceReader<byte> reader, int protocolVersion, NetworkPeerContext peerContext)
      {
         var message = new InitMessage();

         ushort len = reader.ReadUShort();
         message.GlobalFeatures = reader.ReadBytes(len).ToArray();

         len = reader.ReadUShort();
         message.Features = reader.ReadBytes(len).ToArray();

         return message;
      }
   }
}