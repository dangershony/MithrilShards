using System;

namespace NoiseProtocol
{
   public interface IKeyGenerator
   {
      byte[] GenerateKey();
      ReadOnlySpan<byte> GetPublicKey(byte[] privateKey);
   }
}