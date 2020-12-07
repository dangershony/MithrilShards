using System;

namespace NoiseProtocol
{
   public interface IEllipticCurveActions
   {
      ReadOnlySpan<byte> Multiply(byte[] privateKey, ReadOnlySpan<byte> publicKey);
   }
}