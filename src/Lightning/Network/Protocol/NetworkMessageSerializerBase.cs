using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;
using Network.Protocol.Messages;

namespace Network.Protocol
{
   public abstract class NetworkMessageSerializerBase<TMessageType> : NetworkMessageSerializerBase<TMessageType, NetworkPeerContext> where TMessageType : BaseMessage, new()
   {
      protected void MethodOurSerializersMayNeed()
      {
      }

      public void SerializeWithTlv(TMessageType message, int protocolVersion, NetworkPeerContext peerContext, IBufferWriter<byte> output)
      {
         this.Serialize(message, protocolVersion, peerContext, output);

         foreach (KeyValuePair<ulong, TlvRecord> tlvRecord in message.Extension)
         {
         }
      }

      public TMessageType DeserializeWithTlv(ref SequenceReader<byte> reader, int protocolVersion, NetworkPeerContext peerContext)
      {
         TMessageType message = this.Deserialize(ref reader, protocolVersion, peerContext);

         foreach (KeyValuePair<ulong, TlvRecord> tlvRecord in message.Extension)
         {
         }

         return message;
      }
   }
}