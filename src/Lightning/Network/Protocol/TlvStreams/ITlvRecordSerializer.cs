using System;
using System.Buffers;

namespace Network.Protocol.TlvStreams
{
   public interface ITlvRecordSerializer
   {
      Type GetRecordType();

      ulong RecordTlvType { get; }

      void Serialize(TlvRecord message, IBufferWriter<byte> output);

      TlvRecord Deserialize(ref SequenceReader<byte> reader);
   }
}