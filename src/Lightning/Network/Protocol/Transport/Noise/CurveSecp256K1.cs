using System;
using NBitcoin;

namespace Network.Protocol.Transport.Noise
{
   internal sealed class CurveSecp256K1 : IDh
   {
      public int DhLen { get; } = 33;

      public KeyPair GenerateKeyPair()
      {
         var key = new Key();

         return new KeyPair(key.ToBytes(), key.PubKey.ToBytes());
      }

      public KeyPair GenerateKeyPair(ReadOnlySpan<Byte> privateKey)
      {
         var key = new Key(privateKey.ToArray());

         return new KeyPair(privateKey.ToArray(), key.PubKey.ToBytes());
      }

      public void Dh(KeyPair keyPair, ReadOnlySpan<Byte> publicKey, Span<Byte> sharedKey)
      {
         var pubKey = new PubKey(publicKey.ToArray());

         var privateKey = new Key(keyPair.PrivateKey);

         new Span<byte>(pubKey.GetSharedSecret(privateKey))
             .CopyTo(sharedKey);
      }
   }
}