using System;

namespace Network.Protocol.Transport.NewNoise
{
   public interface IHkdf
   {
      void ExtractAndExpand(
         ReadOnlySpan<byte> chainingKey,
         ReadOnlySpan<byte> inputKeyMaterial,
         Span<byte> output);
   }
}