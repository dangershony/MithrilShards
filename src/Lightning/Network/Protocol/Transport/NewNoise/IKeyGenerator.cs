using System;

namespace Network.Protocol.Transport.NewNoise
{
   public interface IKeyGenerator
   {
      Span<byte> GenerateKey();
      ReadOnlySpan<byte> GetPublicKey(byte[] privateKey);
   }
}