using System;
using System.Buffers;
using Network.Protocol.Messages;
using Network.Protocol.Types;

namespace Network.Protocol.Serialization
{
   public interface ITlvRecordSerializer
   {
      Type GetRecordType();

      ulong RecordTlvType { get; }

      void Serialize(TlvRecord message, IBufferWriter<byte> output);

      TlvRecord Deserialize(ref SequenceReader<byte> reader);
   }
}