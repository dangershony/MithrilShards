using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using Network.Protocol.Types;

namespace Network.Protocol.Serialization.Serializers.Messages
{
   public class NetworksTlvSerializer : ITlvRecordSerializer
   {
      public Type GetRecordType() => typeof(NetworksTlvSerializer);

      public ulong RecordTlvType
      {
         get { return 1; }
      }

      public void Serialize(TlvRecord message, IBufferWriter<byte> output)
      {
         // TODO
      }

      public TlvRecord Deserialize(ref SequenceReader<byte> reader)
      {
         reader.Advance(reader.Remaining);

         // TODO

         return new NetworksTlvRecord();
      }
   }
}