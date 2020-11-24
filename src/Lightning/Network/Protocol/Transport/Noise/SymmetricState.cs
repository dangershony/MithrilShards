using System;
using System.Diagnostics;

namespace Network.Protocol.Transport.Noise
{
	/// <summary>
	/// A SymmetricState object contains a CipherState plus ck (a chaining
	/// key of HashLen bytes) and h (a hash output of HashLen bytes).
	/// </summary>
	internal sealed class SymmetricState<CipherType, DhType, HashType> : IDisposable
		where CipherType : Cipher, new()
		where DhType : Dh, new()
		where HashType : Hash, new()
	{
		private readonly Cipher cipher = new CipherType();
		private readonly DhType dh = new DhType();
		private readonly Hash hash = new HashType();
		private readonly Hkdf<HashType> hkdf = new Hkdf<HashType>();
		private readonly CipherState<CipherType> state = new CipherState<CipherType>();
		private readonly byte[] ck;
		private readonly byte[] h;
		private bool disposed;

		/// <summary>
		/// Initializes a new SymmetricState with an
		/// arbitrary-length protocolName byte sequence.
		/// </summary>
		public SymmetricState(ReadOnlySpan<byte> protocolName)
		{
			int length = this.hash.HashLen;

			this.ck = new byte[length];
			this.h = new byte[length];

			if (protocolName.Length <= length)
			{
				protocolName.CopyTo(this.h);
			}
			else
			{
				this.hash.AppendData(protocolName);
				this.hash.GetHashAndReset(this.h);
			}

			Array.Copy(this.h, this.ck, length);
		}

		/// <summary>
		/// Sets ck, tempK = HKDF(ck, inputKeyMaterial, 2).
		/// If HashLen is 64, then truncates tempK to 32 bytes.
		/// Calls InitializeKey(tempK).
		/// </summary>
		public void MixKey(ReadOnlySpan<byte> inputKeyMaterial)
		{
			int length = inputKeyMaterial.Length;
			Debug.Assert(length == 0 || length == Aead.KeySize || length == this.dh.DhLen);

			Span<byte> output = stackalloc byte[2 * this.hash.HashLen];
			this.hkdf.ExtractAndExpand2(this.ck, inputKeyMaterial, output);
			
			output.Slice(0, this.hash.HashLen).CopyTo(this.ck);

			var tempK = output.Slice(this.hash.HashLen, Aead.KeySize);
			this.state.InitializeKey(tempK);
		}

		/// <summary>
		/// Sets h = HASH(h || data).
		/// </summary>
		public void MixHash(ReadOnlySpan<byte> data)
		{
			this.hash.AppendData(this.h);
			this.hash.AppendData(data);
			this.hash.GetHashAndReset(this.h);
		}

		/// <summary>
		/// Sets ck, tempH, tempK = HKDF(ck, inputKeyMaterial, 3).
		/// Calls MixHash(tempH).
		/// If HashLen is 64, then truncates tempK to 32 bytes.
		/// Calls InitializeKey(tempK).
		/// </summary>
		public void MixKeyAndHash(ReadOnlySpan<byte> inputKeyMaterial)
		{
			int length = inputKeyMaterial.Length;
			Debug.Assert(length == 0 || length == Aead.KeySize || length == this.dh.DhLen);

			Span<byte> output = stackalloc byte[3 * this.hash.HashLen];
			this.hkdf.ExtractAndExpand3(this.ck, inputKeyMaterial, output);

			output.Slice(0, this.hash.HashLen).CopyTo(this.ck);

			var tempH = output.Slice(this.hash.HashLen, this.hash.HashLen);
			var tempK = output.Slice(2 * this.hash.HashLen, Aead.KeySize);

			this.MixHash(tempH);
			this.state.InitializeKey(tempK);
		}

		/// <summary>
		/// Returns h. This function should only be called at the end of
		/// a handshake, i.e. after the Split() function has been called.
		/// </summary>
		public byte[] GetHandshakeHash()
		{
			return this.h;
		}

		/// <summary>
		/// Should be called after handshake is complete as the lightning key rotation requires the chaining key
		/// </summary>
		public byte[] GetChainingKey()
		{
			return this.ck;
		}

		/// <summary>
		/// Sets ciphertext = EncryptWithAd(h, plaintext),
		/// calls MixHash(ciphertext), and returns ciphertext.
		/// </summary>
		public int EncryptAndHash(ReadOnlySpan<byte> plaintext, Span<byte> ciphertext)
		{
			int bytesWritten = this.state.EncryptWithAd(this.h, plaintext, ciphertext);
			this.MixHash(ciphertext.Slice(0, bytesWritten));

			return bytesWritten;
		}

		/// <summary>
		/// Sets plaintext = DecryptWithAd(h, ciphertext),
		/// calls MixHash(ciphertext), and returns plaintext.
		/// </summary>
		public int DecryptAndHash(ReadOnlySpan<byte> ciphertext, Span<byte> plaintext)
		{
			var bytesRead = this.state.DecryptWithAd(this.h, ciphertext, plaintext);
			this.MixHash(ciphertext);

			return bytesRead;
		}

		/// <summary>
		/// Returns a pair of CipherState objects for encrypting transport messages.
		/// </summary>
		public (CipherState<CipherType> c1, CipherState<CipherType> c2) Split()
		{
			Span<byte> output = stackalloc byte[2 * this.hash.HashLen];
			this.hkdf.ExtractAndExpand2(this.ck, null, output);

			var tempK1 = output.Slice(0, Aead.KeySize);
			var tempK2 = output.Slice(this.hash.HashLen, Aead.KeySize);

			var c1 = new CipherState<CipherType>(this.ck,this.hkdf);
			var c2 = new CipherState<CipherType>(this.ck,this.hkdf);

			c1.InitializeKey(tempK1);
			c2.InitializeKey(tempK2);

			return (c1, c2);
		}

		/// <summary>
		/// Returns true if k is non-empty, false otherwise.
		/// </summary>
		public bool HasKey()
		{
			return this.state.HasKey();
		}

		public void Dispose()
		{
			if (!this.disposed)
			{
				this.hash.Dispose();
				this.hkdf.Dispose();
				this.state.Dispose();
				Utilities.ZeroMemory(this.ck);
				this.disposed = true;
			}
		}
	}
}
