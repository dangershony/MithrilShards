using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Network.Protocol.Types;

namespace Network.Protocol.Serialization
{
   public interface ITlvStreamSerializer
   {
      bool TryGetType(ulong recordType, [MaybeNullWhen(false)] out ITlvRecordSerializer tlvRecordSerializer);

      void SerializeTlvStream(TlVStream? message, IBufferWriter<byte> output);

      TlVStream? DeserializeTlvStream(ref SequenceReader<byte> reader);
   }
}