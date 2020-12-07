using System;

namespace NoiseProtocol
{
   public interface IHashFunction
   {
      void Hash(Span<byte> span,Span<byte> output);
      void Hash(Span<byte> first,Span<byte> second,Span<byte> output);
      void Hash(ReadOnlySpan<byte> first, ReadOnlySpan<byte> second, ReadOnlySpan<byte> third, Span<byte> output);
   }
}