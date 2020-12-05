using System;

namespace Network.Protocol.Transport.NewNoise
{
   public interface IEllipticCurveActions
   {
      ReadOnlySpan<byte> Multiply(byte[] privateKey, ReadOnlySpan<byte> publicKey);
   }
}