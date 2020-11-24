using System;
using System.Diagnostics;

namespace Network.Protocol.Transport.Noise
{
	/// <summary>
	/// A CipherState can encrypt and decrypt data based on its variables k
	/// (a cipher key of 32 bytes) and n (an 8-byte unsigned integer nonce).
	/// </summary>
	internal sealed class CipherState<CipherType> : IDisposable where CipherType : Cipher, new()
	{
		private const ulong MaxNonce = UInt64.MaxValue;

		private static readonly byte[] zeroLen = new byte[0];
		private static readonly byte[] zeros = new byte[32];

		private readonly CipherType cipher = new CipherType();
		private byte[] k;
		private ulong n;
		private bool disposed;
		
		private readonly byte[] _ck = new byte[32];
		private readonly IHkdf _hkdf;

		public CipherState()
		{ }
		
		public CipherState(byte[] ck, IHkdf hkdf)
		{
			ck.CopyTo(this._ck.AsSpan());
			this._hkdf = hkdf;
		}

		/// <summary>
		/// Sets k = key. Sets n = 0.
		/// </summary>
		public void InitializeKey(ReadOnlySpan<byte> key)
		{
			Debug.Assert(key.Length == Aead.KeySize);

			this.k = this.k ?? new byte[Aead.KeySize];
			key.CopyTo(this.k);

			this.n = 0;
		}

		/// <summary>
		/// Returns true if k is non-empty, false otherwise.
		/// </summary>
		public bool HasKey()
		{
			return this.k != null;
		}

		/// <summary>
		/// Sets n = nonce. This function is used for handling out-of-order transport messages.
		/// </summary>
		public void SetNonce(ulong nonce)
		{
			this.n = nonce;
		}
		
		
		public ulong GetNonce()
		{
			return this.n;
		}

		/// <summary>
		/// If k is non-empty returns ENCRYPT(k, n++, ad, plaintext).
		/// Otherwise copies the plaintext to the ciphertext parameter
		/// and returns the length of the plaintext.
		/// </summary>
		public int EncryptWithAd(ReadOnlySpan<byte> ad, ReadOnlySpan<byte> plaintext, Span<byte> ciphertext)
		{
			if (this.n == MaxNonce)
			{
				throw new OverflowException("Nonce has reached its maximum value.");
			}

			if (this.k == null)
			{
				plaintext.CopyTo(ciphertext);
				return plaintext.Length;
			}
			
			var result = this.cipher.Encrypt(this.k, this.n++, ad, plaintext, ciphertext);

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
			if (this.n == MaxNonce)
			{
				throw new OverflowException("Nonce has reached its maximum value.");
			}

			if (this.k == null)
			{
				ciphertext.CopyTo(plaintext);
				return ciphertext.Length;
			}

			int bytesRead = this.cipher.Decrypt(this.k, this.n, ad, ciphertext, plaintext);
			++this.n;

			return bytesRead;
		}

		/// <summary>
		/// Sets k = REKEY(k).
		/// </summary>
		public void Rekey()
		{
			Debug.Assert(this.HasKey());
			
			Span<byte> key = stackalloc byte[Aead.KeySize + Aead.TagSize];
			this.cipher.Encrypt(this.k, MaxNonce, zeroLen, zeros, key);
			
			this.k ??= new byte[Aead.KeySize];
			key.Slice(Aead.KeySize).CopyTo(this.k);
		}
		
		/// <summary>
		/// Sets k to a new key generated with HKDF of chaining key and current k
		/// </summary>
		public void KeyRecycle()
		{
			Debug.Assert(this.HasKey());

			Span<byte> keys = stackalloc byte[Aead.KeySize * 2];
			this._hkdf.ExtractAndExpand2(this._ck, this.k, keys);

			// set new chaining key
			keys.Slice(0,Aead.KeySize)
				.CopyTo(this._ck);
			
			// set new key
			keys.Slice(Aead.KeySize)
				.CopyTo(this.k);
			
			this.n = 0;
		}

		public void Dispose()
		{
			if (!this.disposed)
			{
				Utilities.ZeroMemory(this.k);
				this.disposed = true;
			}
		}
	}
}
