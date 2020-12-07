using System;

namespace NoiseProtocol
{
   public interface IHashFunction
   {
      void Hash(ReadOnlySpan<byte> span,Span<byte> output);
      void Hash(ReadOnlySpan<byte> first,ReadOnlySpan<byte> second,Span<byte> output);
      void Hash(ReadOnlySpan<byte> first, ReadOnlySpan<byte> second, ReadOnlySpan<byte> third, Span<byte> output);
   }
}