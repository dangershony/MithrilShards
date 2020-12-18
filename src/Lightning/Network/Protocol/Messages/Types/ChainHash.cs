using System;

namespace Network.Protocol.Messages.Types
{
   public class ChainHash
   {
      readonly byte[] _value;
      
      public ChainHash(byte[] value)
      {
         if (value.Length > 32)
            throw new ArgumentOutOfRangeException(nameof(value));
            
         _value = value;
      }

      public static implicit operator byte[](ChainHash hash) => hash._value;
      public static explicit operator ChainHash(byte[] bytes) => new ChainHash(bytes);
      public static explicit operator ChainHash(ReadOnlySpan<byte> bytes) => new ChainHash(bytes.ToArray());
   }
}