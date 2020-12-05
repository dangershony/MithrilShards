using System;
using Network.Protocol.Transport.Noise;

namespace Network.Protocol.Transport.NewNoise
{
   public class AeadConstruction : IAeadConstruction
   {
      byte[] _key = new byte[32];
      ulong _nonce;
      readonly ChaCha20Poly1305 _cha20Poly1305;

      public AeadConstruction()
      {
         _cha20Poly1305 = new ChaCha20Poly1305();
      }

      public void SetKey(Span<byte> key)
      {
         key.CopyTo(_key);
         _nonce = 0;
      } 

      public int EncryptWithAd(ReadOnlySpan<byte> ad, ReadOnlySpan<byte> plaintext, Span<byte> ciphertext)
      {
         return _cha20Poly1305.Encrypt(_key, _nonce++, ad, plaintext, ciphertext);
      }

      public int DecryptWithAd(ReadOnlySpan<byte> ad, ReadOnlySpan<byte> ciphertext, Span<byte> plaintext)
      {
         return _cha20Poly1305.Decrypt(_key, _nonce++, ad, ciphertext, plaintext);
      }
   }
}