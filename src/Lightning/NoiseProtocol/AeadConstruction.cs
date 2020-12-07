using System;
using System.Buffers.Binary;
using System.Diagnostics;
using NaCl.Core;

namespace NoiseProtocol
{
   public class AeadConstruction : IAeadConstruction
   {
      byte[] _key = new byte[32];
      ulong _nonce;

      public void SetKey(Span<byte> key)
      {
         key.CopyTo(_key);
         _nonce = 0;
      } 

      public int EncryptWithAd(ReadOnlySpan<byte> ad, ReadOnlySpan<byte> plaintext, Span<byte> ciphertext)
      {
         Debug.Assert(_key.Length == Aead.KEY_SIZE);
         Debug.Assert(ciphertext.Length >= plaintext.Length + Aead.TAG_SIZE);

         Span<byte> nonce = stackalloc byte[Aead.NONCE_SIZE];
         BinaryPrimitives.WriteUInt64LittleEndian(nonce.Slice(4), _nonce);

         var cipher = new ChaCha20Poly1305(_key);

         var cipherTextOutput = ciphertext.Slice(0, plaintext.Length);
         var tag = ciphertext.Slice(plaintext.Length, Aead.TAG_SIZE);

         cipher.Encrypt(nonce, plaintext.ToArray(), cipherTextOutput, tag, ad.ToArray());

         _nonce++;
         
         return cipherTextOutput.Length + tag.Length;
      }

      public int DecryptWithAd(ReadOnlySpan<byte> ad, ReadOnlySpan<byte> ciphertext, Span<byte> plaintext)
      {
         Debug.Assert(_key.Length == Aead.KEY_SIZE);
         Debug.Assert(ciphertext.Length >= Aead.TAG_SIZE);
         Debug.Assert(plaintext.Length >= ciphertext.Length - Aead.TAG_SIZE);

         Span<byte> nonce = stackalloc byte[Aead.NONCE_SIZE];
         BinaryPrimitives.WriteUInt64LittleEndian(nonce.Slice(4), _nonce);

         var cipher = new ChaCha20Poly1305(_key);

         var cipherTextWithoutTag = ciphertext.Slice(0, ciphertext.Length - Aead.TAG_SIZE);
         var tag = ciphertext.Slice(ciphertext.Length - Aead.TAG_SIZE);

         cipher.Decrypt(nonce, cipherTextWithoutTag, tag, plaintext, ad);

         _nonce++;
         
         return cipherTextWithoutTag.Length;
      }
   }
}