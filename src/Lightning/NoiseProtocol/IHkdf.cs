using System;

namespace NoiseProtocol
{
   public interface IHkdf
   {
      void ExtractAndExpand(
         ReadOnlySpan<byte> chainingKey,
         ReadOnlySpan<byte> inputKeyMaterial,
         Span<byte> output);
   }
}