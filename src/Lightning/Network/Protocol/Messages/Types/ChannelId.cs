using System;

namespace Network.Protocol.Messages.Types
{
   public struct ChannelId
   {
      readonly byte[] _value;
      
      public ChannelId(byte[] value)
      {
         if (value.Length > 32)
            throw new ArgumentOutOfRangeException(nameof(value));
            
         _value = value;
      }

      public static implicit operator byte[](ChannelId hash) => hash._value;
      public static explicit operator ChannelId(byte[] bytes) => new ChannelId(bytes);   }
}