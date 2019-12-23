﻿using System;
using System.Buffers;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Types {
   /// <summary>
   ///  64 bit unsigned integer (int64_t).
   /// </summary>
   public class ULong : ISerializableProtocolTypeEndiannessAware<ULong> {
      public string InternalName => "uint64_t";

      public int Length => 8;

      public void Deserialize(SequenceReader<byte> data, bool isLittleEndian) {
         throw new NotImplementedException();
      }

      public byte[] Serialize(bool isLittleEndian) {
         throw new NotImplementedException();
      }
   }
}
