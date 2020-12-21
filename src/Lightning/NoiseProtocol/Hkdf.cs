using System;
using System.Diagnostics;

namespace NoiseProtocol
{
   public class Hkdf : IHkdf, IDisposable
   {
      static readonly byte[] _one = {1};
      private static readonly byte[] _two = { 2 };

      private readonly IHashWithState _inner;
      private readonly IHashWithState _outer;
      private bool _disposed;

      public Hkdf(IHashWithState inner, IHashWithState outer)
      {
         _inner = inner;
         _outer = outer;
      }

      /// <summary>
      /// Takes a chainingKey byte sequence of length HashLen,
      /// and an inputKeyMaterial byte sequence with length
      /// either zero bytes, 32 bytes, or DhLen bytes. Writes a
      /// byte sequences of length 2 * HashLen into output parameter.
      /// </summary>
      public void ExtractAndExpand(
         ReadOnlySpan<byte> chainingKey,
         ReadOnlySpan<byte> inputKeyMaterial,
         Span<byte> output)
      {
         int hashLen = _inner.HashLen;

         Debug.Assert(chainingKey.Length == hashLen);
         Debug.Assert(output.Length == 2 * hashLen);

         Span<byte> tempKey = stackalloc byte[hashLen];
         HmacHash(chainingKey, tempKey, inputKeyMaterial);

         var output1 = output.Slice(0, hashLen);
         HmacHash(tempKey, output1, _one);

         var output2 = output.Slice(hashLen, hashLen);
         HmacHash(tempKey, output2, output1, _two);
      }

      private void HmacHash(
         ReadOnlySpan<byte> key,
         Span<byte> hmac,
         ReadOnlySpan<byte> data1 = default,
         ReadOnlySpan<byte> data2 = default)
      {
         Debug.Assert(key.Length == _inner.HashLen);
         Debug.Assert(hmac.Length == _inner.HashLen);

         int blockLen = _inner.BlockLen;

         Span<byte> ipad = stackalloc byte[blockLen];
         Span<byte> opad = stackalloc byte[blockLen];

         key.CopyTo(ipad);
         key.CopyTo(opad);

         for (int i = 0; i < blockLen; ++i)
         {
            ipad[i] ^= 0x36;
            opad[i] ^= 0x5C;
         }

         _inner.AppendData(ipad);
         _inner.AppendData(data1);
         _inner.AppendData(data2);
         _inner.GetHashAndReset(hmac);

         _outer.AppendData(opad);
         _outer.AppendData(hmac);
         _outer.GetHashAndReset(hmac);
      }

      public void Dispose()
      {
         if (_disposed) 
            return;
         
         DisposeHash(_inner);
         DisposeHash(_outer);
         _disposed = true;
      }

      private static void DisposeHash(object hashWithState)
      {
         var hash = hashWithState as IDisposable;

         hash?.Dispose();
      }
   }
}