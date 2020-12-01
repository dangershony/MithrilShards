using System;
using System.Buffers;
using Network.Protocol.TlvStreams.TlvRecords;

namespace Network.Protocol.TlvStreams.Serializers
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