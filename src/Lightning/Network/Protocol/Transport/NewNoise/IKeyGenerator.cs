using System;

namespace Network.Protocol.Transport.NewNoise
{
   public interface IKeyGenerator
   {
      byte[] GenerateKey();
      ReadOnlySpan<byte> GetPublicKey(byte[] privateKey);
   }
}