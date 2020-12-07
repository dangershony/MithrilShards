using System;

namespace NoiseProtocol
{
   public class Hkdf : IHkdf
   {
      private static readonly byte[] _one = new byte[] { 1 };
      private static readonly byte[] _two = new byte[] { 2 };

      readonly IHashFunction _inner,_outer;

      OldHkdf _oldHkdf = new OldHkdf();

      public Hkdf(IHashFunction inner, IHashFunction outer)
      {
         _inner = inner;
         _outer = outer;
      }

      static int HashLen => 32;
      static int BlockLen => 64;
      
      public void ExtractAndExpand(ReadOnlySpan<byte> chainingKey, ReadOnlySpan<byte> inputKeyMaterial,
         Span<byte> output) 
      {
         _oldHkdf.ExtractAndExpand2(chainingKey,inputKeyMaterial,output);
         // Span<byte> tempKey = stackalloc byte[HashLen];
         // HmacHash(chainingKey, tempKey, inputKeyMaterial);
         //
         // var output1 = output.Slice(0, HashLen);
         // HmacHash(tempKey, output1, _one);
         //
         // var output2 = output.Slice(HashLen, HashLen);
         // HmacHash(tempKey, output2, output1, _two);
      }

      private void HmacHash(
         ReadOnlySpan<byte> key,
         Span<byte> hmac,
         ReadOnlySpan<byte> data1 = default,
         ReadOnlySpan<byte> data2 = default)
      {
         Span<byte> ipad = stackalloc byte[BlockLen];
         Span<byte> opad = stackalloc byte[BlockLen];

         key.CopyTo(ipad);
         key.CopyTo(opad);

         for (int i = 0; i < BlockLen; ++i)
         {
            ipad[i] ^= 0x36;
            opad[i] ^= 0x5C;
         }

         _inner.Hash(ipad, data1, data2, hmac);

         _outer.Hash(opad,hmac,hmac);
      }
   }
}