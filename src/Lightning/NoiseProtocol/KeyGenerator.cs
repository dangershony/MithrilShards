using System;
using System.Linq;
using NBitcoin;

namespace NoiseProtocol
{
   public class KeyGenerator : IKeyGenerator
   {
      public byte[] GenerateKey() => new Key().ToBytes();

      public ReadOnlySpan<byte> GetPublicKey(byte[] privateKey) => new Key(privateKey).PubKey.ToBytes();
   }
}