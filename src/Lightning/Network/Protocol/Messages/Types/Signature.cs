using System;

namespace Network.Protocol.Messages.Types
{
   public struct Signature
   {
      public const ushort SIGNATURE_LENGTH = 64;
      
      readonly byte[] _value;
      
      public Signature(byte[] value)
      {
         if (value.Length > 64)
            throw new ArgumentOutOfRangeException(nameof(value));
            
         _value = value;
      }

      public static implicit operator byte[](Signature hash) => hash._value;
      public static explicit operator Signature(byte[] bytes) => new Signature(bytes);
      public static explicit operator Signature(ReadOnlySpan<byte> bytes) => new Signature(bytes.ToArray());
   }
}