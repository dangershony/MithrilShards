using System;

namespace Network.Protocol.Transport.NewNoise
{
   public interface IHasher
   {
      void Hash(Span<byte> span,Span<byte> output);
      void Hash(Span<byte> first,Span<byte> second,Span<byte> output);
   }
}