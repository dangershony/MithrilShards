using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Network.Protocol.Messages.Tlv
{
   public interface ITlvHandler
   {
      TlvSequence ReadTlvMessage(SequenceReader<byte> sequenceReader);

      void WriteTlvMessage(TlvSequence tlvSequence, IBufferWriter<byte> buffer);
   }

   public class TlvHandler : ITlvHandler
   {
      private const int MAX_RECORD_SIZE = 65535; // 65KB

      public TlvSequence ReadTlvMessage(SequenceReader<byte> sequenceReader)
      {
         var tlvMessage = new TlvSequence();
         TlvRecord lasRecord = null;

         while (sequenceReader.Remaining > 0)
         {
            var tlvRecord = new TlvRecord { Type = sequenceReader.ReadBigSize(), };

            if (lasRecord == null)
            {
               lasRecord = tlvRecord;
            }
            else
            {
               if (tlvRecord.Type > lasRecord.Type)
                  throw new SerializationException("Tlv records not canonical");
            }

            tlvRecord.Size = sequenceReader.ReadBigSize();

            if (tlvRecord.Size > MAX_RECORD_SIZE)
               throw new SerializationException("Record is too large");

            if (tlvRecord.Size > (ulong)sequenceReader.Remaining)
               throw new SerializationException("Not enough bytes to read the tlv record");

            tlvRecord.Value = sequenceReader.Sequence.Slice(sequenceReader.Position, (int)tlvRecord.Size);

            tlvMessage.TlvRecords.Add(tlvRecord.Type, tlvRecord);
         }

         return tlvMessage;
      }

      public void WriteTlvMessage(TlvSequence tlvSequence, IBufferWriter<byte> buffer)
      {
         TlvRecord lasRecord = null;

         foreach (KeyValuePair<ulong, TlvRecord> tlvRecord in tlvSequence.TlvRecords)
         {
            buffer.WriteBigSize(tlvRecord.Value.Type);

            if (lasRecord == null)
            {
               lasRecord = tlvRecord.Value;
            }
            else
            {
               if (tlvRecord.Value.Type < lasRecord.Type)
                  throw new SerializationException("Tlv records not canonical");
            }

            if (tlvRecord.Value.Size > MAX_RECORD_SIZE)
               throw new SerializationException("Record is too large");

            buffer.WriteBigSize(tlvRecord.Value.Size);

            foreach (ReadOnlyMemory<byte> memory in tlvRecord.Value.Value)
               buffer.Write(memory.Span);
         }
      }
   }
}