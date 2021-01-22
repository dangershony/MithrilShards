using System;

namespace Network.Protocol.Messages.Types
{
   public class ShortChannelId
   {
      private readonly byte[] _value;

      public ShortChannelId(byte[] value)
      {
         if (value.Length > 8)
            throw new ArgumentOutOfRangeException(nameof(value));

         _value = value;
      }

      public static implicit operator byte[](ShortChannelId hash) => hash._value;

      public static explicit operator ShortChannelId(byte[] bytes) => new ShortChannelId(bytes);
   }
}