using System;

namespace Network.Protocol.Messages.Types
{
   public class CompressedSignature
   {
      public const ushort SIGNATURE_LENGTH = 64;
      
      readonly byte[] _value;

      // public Span<byte> R => _value.AsSpan(0, 32);
      // public Span<byte> S => _value.AsSpan(32);
      
      public CompressedSignature()
      {
         _value = new byte[0];
      }
      
      public CompressedSignature(byte[] value)
      {
         if (value.Length != 64)
            throw new ArgumentOutOfRangeException(nameof(value));
            
         _value = value;
      }

      public static implicit operator byte[](CompressedSignature hash) => hash._value;
      public static explicit operator CompressedSignature(byte[] bytes) => new CompressedSignature(bytes);
      public static explicit operator CompressedSignature(ReadOnlySpan<byte> bytes) => new CompressedSignature(bytes.ToArray());
   }
}