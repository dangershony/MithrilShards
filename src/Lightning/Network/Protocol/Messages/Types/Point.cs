using System;

namespace Network.Protocol.Messages.Types
{
   public struct Point
   {
      public const ushort POINT_LENGTH = 33;
      
      readonly byte[] _value;
      
      public Point(byte[] value)
      {
         if (value.Length > POINT_LENGTH)
            throw new ArgumentOutOfRangeException(nameof(value));
            
         _value = value;
      }

      public static implicit operator byte[](Point hash) => hash._value;
      public static explicit operator Point(byte[] bytes) => new Point(bytes);
      public static explicit operator Point(ReadOnlySpan<byte> bytes) => new Point(bytes.ToArray());
   }
}