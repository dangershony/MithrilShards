using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace Network.Protocol.Serialization.Serializers.Types
{
   public class TlvStreamSerializer : IProtocolTypeSerializer<TlvStreamSerializer>
   {
      readonly IRecordSerializerManager recordSerializerManager;

      public TlvStreamSerializer(IRecordSerializerManager recordSerializerManager)
      {
         this.recordSerializerManager = recordSerializerManager;
      }

      public TlvStreamSerializer Deserialize(ref SequenceReader<byte> reader, int protocolVersion, ProtocolTypeSerializerOptions options = null)
      {
      }

      public int Serialize(TlvStreamSerializer typeInstance, int protocolVersion, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions options = null)
      {
      }
   }
}
