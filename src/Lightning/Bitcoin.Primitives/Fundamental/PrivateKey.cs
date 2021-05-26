using System;

namespace Bitcoin.Primitives.Fundamental
{
   public class PrivateKey
   {
      protected readonly byte[] _value;

      public PrivateKey(byte[] value)
      {
         if (value.Length > 32)
            throw new ArgumentOutOfRangeException(nameof(value));

         _value = value;
      }

      public static implicit operator ReadOnlySpan<byte>(PrivateKey hash) => hash._value;

      public static implicit operator byte[](PrivateKey hash) => hash._value;

      public static explicit operator PrivateKey(byte[] bytes) => new PrivateKey(bytes);

      public static explicit operator PrivateKey(ReadOnlySpan<byte> bytes) => new PrivateKey(bytes.ToArray());
   }

   public class Secret : PrivateKey
   {
      public Secret(byte[] value) : base(value)
      {
      }
   }

   public class Preimage : PrivateKey
   {
      public Preimage(byte[] value) : base(value)
      {
      }

      public static implicit operator byte[](Preimage hash) => hash._value;
   }
}