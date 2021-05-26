using System;

namespace Bitcoin.Primitives.Fundamental
{
   public class TransactionSignature
   {
      protected readonly byte[] _value;

      public TransactionSignature(byte[] value)
      {
         if (value.Length > 74)
            throw new ArgumentOutOfRangeException(nameof(value));

         _value = value;
      }

      public ReadOnlySpan<byte> GetSpan()
      {
         return _value.AsSpan();
      }

      public static implicit operator ReadOnlySpan<byte>(TransactionSignature hash) => hash._value;

      public static implicit operator byte[](TransactionSignature hash) => hash._value;

      public static explicit operator TransactionSignature(byte[] bytes) => new TransactionSignature(bytes);

      public static explicit operator TransactionSignature(ReadOnlySpan<byte> bytes) => new TransactionSignature(bytes.ToArray());
   }
}