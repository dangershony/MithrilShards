using System;
using Network.Protocol.Transport.Noise;

namespace Network.Test.Protocol.Transport.Noise
{
   public class DhWrapperWithDefinedEphemeralKey : IDh
   {
      private readonly KeyPair _pair;
      private readonly CurveSecp256K1 _curveSecp256K1;

      public DhWrapperWithDefinedEphemeralKey(KeyPair pair)
      {
         _pair = pair;
         _curveSecp256K1 = new CurveSecp256K1();
      }

      public int DhLen => _curveSecp256K1.DhLen;

      public KeyPair GenerateKeyPair()
      {
         return _pair;
      }

      public KeyPair GenerateKeyPair(ReadOnlySpan<byte> privateKey)
      {
         return _curveSecp256K1.GenerateKeyPair(privateKey);
      }

      public void Dh(KeyPair keyPair, ReadOnlySpan<byte> publicKey, Span<byte> sharedKey)
      {
         _curveSecp256K1.Dh(keyPair, publicKey, sharedKey);
      }
   }
}