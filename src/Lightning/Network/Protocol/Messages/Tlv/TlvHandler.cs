using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace Network.Protocol.Messages.Tlv
{
   public interface ITlvHandler
   {
      TlvMessage ReadTlvMessage(SequenceReader<byte> sequenceReader);

      void WriteTlvMessage(TlvMessage tlvMessage, IBufferWriter<byte> buffer);
   }

   public class TlvHandler : ITlvHandler
   {
      public TlvMessage ReadTlvMessage(SequenceReader<byte> sequenceReader)
      {
         return new TlvMessage();
      }

      public void WriteTlvMessage(TlvMessage tlvMessage, IBufferWriter<byte> buffer)
      {
      }
   }
}