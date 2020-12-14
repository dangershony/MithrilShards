using System;
using System.Collections.Generic;
using Network.Test.Protocol.Transport.Noise;

namespace NoiseProtocol.Test
{
   internal class FixedKeysGenerator : IKeyGenerator
   {
      readonly byte[] _privateKey;
      readonly Dictionary<string, byte[]> _keys;

      public FixedKeysGenerator(byte[] privateKey, byte[] publicKey)
      {
         _privateKey = privateKey;
         _keys = new Dictionary<string, byte[]> {{privateKey.ToHexString(), publicKey}};
      }

      public FixedKeysGenerator AddKeys(byte[] privateKey, byte[] publicKey)
      {
         _keys.Add(privateKey.ToHexString(),publicKey);
         return this;
      }

      public byte[] GenerateKey() => _privateKey;

      public ReadOnlySpan<byte> GetPublicKey(byte[] privateKey) =>
         _keys[privateKey.ToHexString()];
   }
}