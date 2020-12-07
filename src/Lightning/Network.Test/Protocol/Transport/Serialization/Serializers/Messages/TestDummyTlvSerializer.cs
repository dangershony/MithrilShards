using System;
using System.Buffers;
using Network.Protocol.TlvStreams.TlvRecords;

namespace Network.Protocol.TlvStreams.Serializers
{
   /// <summary>
   /// A dummy tlv serializer for unknown tlv types
   /// </summary>
   public class TestDummyTlvSerializer : ITlvRecordSerializer
   {
      public TestDummyTlvSerializer(ulong recordTlvType)
      {
         RecordTlvType = recordTlvType;
      }

      public Type GetRecordType() => typeof(TestDummyTlvSerializer);

      public ulong RecordTlvType { get; }

      public void Serialize(TlvRecord message, IBufferWriter<byte> output)
      {
         // for now just fill the buffer
         output.Write(message.Payload.AsSpan());
      }

      public TlvRecord Deserialize(ref SequenceReader<byte> reader)
      {
         var result = new TlvRecord { Type = RecordTlvType, Size = (ulong)reader.Remaining };

         result.Payload = reader.ReadBytes((int)reader.Remaining).ToArray();

         return result;
      }
   }
}