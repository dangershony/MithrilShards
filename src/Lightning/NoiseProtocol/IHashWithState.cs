using System;

namespace NoiseProtocol
{
   public interface IHashWithState
   {
      void AppendData(ReadOnlySpan<byte> data);

      void GetHashAndReset(Span<byte> hash);

      int HashLen { get; }
      
      int BlockLen { get; }
   }
}