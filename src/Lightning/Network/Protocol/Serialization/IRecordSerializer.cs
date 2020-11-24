using System;
using System.Buffers;
using Network.Protocol.Messages;

namespace Network.Protocol.Serialization
{
   public interface IRecordSerializer
   {
      Type GetRecordType();

      void Serialize(TlvRecord message, IBufferWriter<byte> output);

      TlvRecord Deserialize(ref SequenceReader<byte> reader);
   }
}
