using System;

namespace NoiseProtocol
{
   public interface ICipherFunction
   {
      void SetKey(ReadOnlySpan<byte> key);
      ReadOnlySpan<byte> GetKey();

      ulong GetNonce();
      
      int EncryptWithAd(ReadOnlySpan<byte> ad, ReadOnlySpan<byte> plaintext, Span<byte> ciphertext);
      int DecryptWithAd(ReadOnlySpan<byte> ad, ReadOnlySpan<byte> ciphertext, Span<byte> plaintext);
   }
}