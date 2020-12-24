using System;
using MithrilShards.Core.DataTypes;

namespace Network.Protocol.Messages.Types
{
   public class ChainHash
   {
      readonly UInt256 _value;
      
      public ChainHash(byte[] value)
      {
         _value = new UInt256(value);
      }

      public static implicit operator byte[](ChainHash hash) => hash._value.GetBytes().ToArray();
      public static explicit operator ChainHash(byte[] bytes) => new ChainHash(bytes);
      public static explicit operator ChainHash(ReadOnlySpan<byte> bytes) => new ChainHash(bytes.ToArray());
   }
}