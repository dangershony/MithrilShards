using System;
using System.Buffers.Binary;
using System.Diagnostics;

namespace Network.Protocol.Transport.Noise
{
	/// <summary>
	/// AEAD_CHACHA20_POLY1305 from <see href="https://tools.ietf.org/html/rfc7539">RFC 7539</see>.
	/// The 96-bit nonce is formed by encoding 32 bits
	/// of zeros followed by little-endian encoding of n.
	/// </summary>
	internal sealed class ChaCha20Poly1305 : Cipher
	{
		public int Encrypt(ReadOnlySpan<byte> k, ulong n, ReadOnlySpan<byte> ad, ReadOnlySpan<byte> plaintext, Span<byte> ciphertext)
		{
			Debug.Assert(k.Length == Aead.KeySize);
			Debug.Assert(ciphertext.Length >= plaintext.Length + Aead.TagSize);

			Span<byte> nonce = stackalloc byte[Aead.NonceSize];
			BinaryPrimitives.WriteUInt64LittleEndian(nonce.Slice(4), n);

			var cipher = new NaCl.Core.ChaCha20Poly1305(k.ToArray());

			var cipherTextOutput = ciphertext.Slice(0, plaintext.Length);
			var tag = ciphertext.Slice(plaintext.Length, Aead.TagSize);
			
			cipher.Encrypt(nonce, plaintext.ToArray(), cipherTextOutput, tag, ad.ToArray());

			return cipherTextOutput.Length + tag.Length;
		}

		public int Decrypt(ReadOnlySpan<byte> k, ulong n, ReadOnlySpan<byte> ad, ReadOnlySpan<byte> ciphertext, Span<byte> plaintext)
		{
			Debug.Assert(k.Length == Aead.KeySize);
			Debug.Assert(ciphertext.Length >= Aead.TagSize);
			Debug.Assert(plaintext.Length >= ciphertext.Length - Aead.TagSize);

			Span<byte> nonce = stackalloc byte[Aead.NonceSize];
			BinaryPrimitives.WriteUInt64LittleEndian(nonce.Slice(4), n);

			var cipher = new NaCl.Core.ChaCha20Poly1305(k.ToArray());

			var cipherTextWithoutTag = ciphertext.Slice(0, ciphertext.Length - Aead.TagSize); 
			var tag = ciphertext.Slice(ciphertext.Length - Aead.TagSize);

			cipher.Decrypt(nonce, cipherTextWithoutTag, tag, plaintext, ad);

			return cipherTextWithoutTag.Length;
		}
	}
}