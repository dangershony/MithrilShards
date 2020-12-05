using System;
using NBitcoin;

namespace Network.Protocol.Transport.NewNoise
{
   public class EllipticCurveActions : IEllipticCurveActions
   {
      public ReadOnlySpan<byte> Multiply(byte[] privateKey, ReadOnlySpan<byte> publicKey) 
         => new PubKey(publicKey.ToArray())
            .GetSharedSecret(new Key(privateKey));
   }
}