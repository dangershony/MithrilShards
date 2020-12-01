using System.Buffers;

namespace Network.Protocol.TlvStreams
{
   public interface ITlvStreamSerializer
   {
      bool TryGetType(ulong recordType, /*[MaybeNullWhen(false)]*/ out ITlvRecordSerializer? tlvRecordSerializer);

      void SerializeTlvStream(TlVStream? message, IBufferWriter<byte> output);

      TlVStream? DeserializeTlvStream(ref SequenceReader<byte> reader);
   }
}