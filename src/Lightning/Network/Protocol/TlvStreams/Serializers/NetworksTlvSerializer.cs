﻿using System;
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
         // for now just fill the buffer
         output.Write(message.Payload.AsSpan());

         // TODO
      }

      public TlvRecord Deserialize(ref SequenceReader<byte> reader)
      {
         var result = new NetworksTlvRecord { Type = RecordTlvType, Size = (ulong)reader.Remaining };

         result.Payload = reader.ReadBytes((int)reader.Remaining).ToArray();

         // TODO

         return result;
      }
   }
}