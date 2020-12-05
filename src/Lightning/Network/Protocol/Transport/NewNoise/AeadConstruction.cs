using System;
using Network.Protocol.Transport.Noise;

namespace Network.Protocol.Transport.NewNoise
{
   public class AeadConstruction : IAeadConstruction
   {
      byte[] _k = new byte[32];
      ulong n;
      ChaCha20Poly1305 _cha20Poly1305;

      public AeadConstruction()
      {
         _cha20Poly1305 = new ChaCha20Poly1305();
      }

      public void SetKey(Span<byte> key)
      {
         key.CopyTo(_k);
         n = 0;
      } 

      public int EncryptWithAd(ReadOnlySpan<byte> ad, ReadOnlySpan<byte> plaintext, Span<byte> ciphertext)
      {
         return _cha20Poly1305.Encrypt(_k, n++, ad, plaintext, ciphertext);
      }

      public int DecryptWithAd(ReadOnlySpan<byte> ad, ReadOnlySpan<byte> ciphertext, Span<byte> plaintext)
      {
         return _cha20Poly1305.Decrypt(_k, n++, ad, ciphertext, plaintext);
      }
   }
}