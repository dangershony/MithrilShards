using System;
using System.Diagnostics;

namespace Network.Protocol.Transport.Noise
{
   /// <summary>
   /// A CipherState can encrypt and decrypt data based on its variables k
   /// (a cipher key of 32 bytes) and n (an 8-byte unsigned integer nonce).
   /// </summary>
   internal sealed class CipherState<TCipherType> : IDisposable where TCipherType : ICipher, new()
   {
      private const ulong MAX_NONCE = UInt64.MaxValue;

      private static readonly byte[] _zeroLen = new byte[0];
      private static readonly byte[] _zeros = new byte[32];

      private readonly TCipherType _cipher = new TCipherType();
      private byte[] _k;
      private ulong _n;
      private bool _disposed;

      private readonly byte[] _ck = new byte[32];
      private readonly IHkdf _hkdf;

      public CipherState()
      { }

      public CipherState(byte[] ck, IHkdf hkdf)
      {
         ck.CopyTo(_ck.AsSpan());
         _hkdf = hkdf;
      }

      /// <summary>
      /// Sets k = key. Sets n = 0.
      /// </summary>
      public void InitializeKey(ReadOnlySpan<byte> key)
      {
         Debug.Assert(key.Length == Aead.KEY_SIZE);

         _k = _k ?? new byte[Aead.KEY_SIZE];
         key.CopyTo(_k);

         _n = 0;
      }

      /// <summary>
      /// Returns true if k is non-empty, false otherwise.
      /// </summary>
      public bool HasKey()
      {
         return _k != null;
      }

      /// <summary>
      /// Sets n = nonce. This function is used for handling out-of-order transport messages.
      /// </summary>
      public void SetNonce(ulong nonce)
      {
         _n = nonce;
      }

      public ulong GetNonce()
      {
         return _n;
      }

      /// <summary>
      /// If k is non-empty returns ENCRYPT(k, n++, ad, plaintext).
      /// Otherwise copies the plaintext to the ciphertext parameter
      /// and returns the length of the plaintext.
      /// </summary>
      public int EncryptWithAd(ReadOnlySpan<byte> ad, ReadOnlySpan<byte> plaintext, Span<byte> ciphertext)
      {
         if (_n == MAX_NONCE)
         {
            throw new OverflowException("Nonce has reached its maximum value.");
         }

         if (_k == null)
         {
            plaintext.CopyTo(ciphertext);
            return plaintext.Length;
         }

         var result = _cipher.Encrypt(_k, _n++, ad, plaintext, ciphertext);

         return result;
      }

      /// <summary>
      /// If k is non-empty returns DECRYPT(k, n++, ad, ciphertext).
      /// Otherwise copies the ciphertext to the plaintext parameter and returns
      /// the length of the ciphertext. If an authentication failure occurs
      /// then n is not incremented and an error is signaled to the caller.
      /// </summary>
      public int DecryptWithAd(ReadOnlySpan<byte> ad, ReadOnlySpan<byte> ciphertext, Span<byte> plaintext)
      {
         if (_n == MAX_NONCE)
         {
            throw new OverflowException("Nonce has reached its maximum value.");
         }

         if (_k == null)
         {
            ciphertext.CopyTo(plaintext);
            return ciphertext.Length;
         }

         int bytesRead = _cipher.Decrypt(_k, _n, ad, ciphertext, plaintext);
         ++_n;

         return bytesRead;
      }

      /// <summary>
      /// Sets k = REKEY(k).
      /// </summary>
      public void Rekey()
      {
         Debug.Assert(HasKey());

         Span<byte> key = stackalloc byte[Aead.KEY_SIZE + Aead.TAG_SIZE];
         _cipher.Encrypt(_k, MAX_NONCE, _zeroLen, _zeros, key);

         _k ??= new byte[Aead.KEY_SIZE];
         key.Slice(Aead.KEY_SIZE).CopyTo(_k);
      }

      /// <summary>
      /// Sets k to a new key generated with HKDF of chaining key and current k
      /// </summary>
      public void KeyRecycle()
      {
         Debug.Assert(HasKey());

         Span<byte> keys = stackalloc byte[Aead.KEY_SIZE * 2];
         _hkdf.ExtractAndExpand2(_ck, _k, keys);

         // set new chaining key
         keys.Slice(0, Aead.KEY_SIZE)
            .CopyTo(_ck);

         // set new key
         keys.Slice(Aead.KEY_SIZE)
            .CopyTo(_k);

         _n = 0;
      }

      public void Dispose()
      {
         if (!_disposed)
         {
            Utilities.ZeroMemory(_k);
            _disposed = true;
         }
      }
   }
}