using System;

namespace Bitcoin.Primitives.Fundamental
{
   public class ShotrChannelId
   {
      private readonly byte[] _value;

      public ShotrChannelId(byte[] value)
      {
         if (value.Length > 8)
            throw new ArgumentOutOfRangeException(nameof(value));

         _value = value;
      }

      public static implicit operator byte[](ShotrChannelId hash) => hash._value;

      public static explicit operator ShotrChannelId(byte[] bytes) => new ShotrChannelId(bytes);
   }
}