using System;
using System.Linq;
using NBitcoin;

namespace Network.Protocol.Transport.NewNoise
{
   public class KeyGenerator : IKeyGenerator
   {
      public static byte[] ToByteArray(string hex)
      {
         if (string.IsNullOrEmpty(hex)) return null;
			
         int startIndex = hex.ToLower().StartsWith("0x") ? 2 : 0;
			
         return Enumerable.Range(startIndex, hex.Length - startIndex)
            .Where(x => x % 2 == 0)
            .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
            .ToArray();
      }
      
      public byte[] GenerateKey() => ToByteArray("0x2222222222222222222222222222222222222222222222222222222222222222");

      public ReadOnlySpan<byte> GetPublicKey(byte[] privateKey) => new Key(privateKey).PubKey.ToBytes();
   }
}