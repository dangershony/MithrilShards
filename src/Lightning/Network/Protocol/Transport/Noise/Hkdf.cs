using System;
using System.Diagnostics;

namespace Network.Protocol.Transport.Noise
{
	/// <summary>
	/// HMAC-based Extract-and-Expand Key Derivation Function, defined in
	/// <see href="https://tools.ietf.org/html/rfc5869">RFC 5869</see>.
	/// </summary>
	internal sealed class Hkdf<HashType> : IHkdf, IDisposable where HashType : Hash, new()
	{
		private static readonly byte[] one = new byte[] { 1 };
		private static readonly byte[] two = new byte[] { 2 };
		private static readonly byte[] three = new byte[] { 3 };

		private readonly HashType inner = new HashType();
		private readonly HashType outer = new HashType();
		private bool disposed;

		/// <summary>
		/// Takes a chainingKey byte sequence of length HashLen,
		/// and an inputKeyMaterial byte sequence with length
		/// either zero bytes, 32 bytes, or DhLen bytes. Writes a
		/// byte sequences of length 2 * HashLen into output parameter.
		/// </summary>
		public void ExtractAndExpand2(
			ReadOnlySpan<byte> chainingKey,
			ReadOnlySpan<byte> inputKeyMaterial,
			Span<byte> output)
		{
			int hashLen = this.inner.HashLen;

			Debug.Assert(chainingKey.Length == hashLen);
			Debug.Assert(output.Length == 2 * hashLen);

			Span<byte> tempKey = stackalloc byte[hashLen];
			this.HmacHash(chainingKey, tempKey, inputKeyMaterial);

			var output1 = output.Slice(0, hashLen);
			this.HmacHash(tempKey, output1, one);

			var output2 = output.Slice(hashLen, hashLen);
			this.HmacHash(tempKey, output2, output1, two);
		}

		/// <summary>
		/// Takes a chainingKey byte sequence of length HashLen,
		/// and an inputKeyMaterial byte sequence with length
		/// either zero bytes, 32 bytes, or DhLen bytes. Writes a
		/// byte sequences of length 3 * HashLen into output parameter.
		/// </summary>
		public void ExtractAndExpand3(
			ReadOnlySpan<byte> chainingKey,
			ReadOnlySpan<byte> inputKeyMaterial,
			Span<byte> output)
		{
			int hashLen = this.inner.HashLen;

			Debug.Assert(chainingKey.Length == hashLen);
			Debug.Assert(output.Length == 3 * hashLen);

			Span<byte> tempKey = stackalloc byte[hashLen];
			this.HmacHash(chainingKey, tempKey, inputKeyMaterial);

			var output1 = output.Slice(0, hashLen);
			this.HmacHash(tempKey, output1, one);

			var output2 = output.Slice(hashLen, hashLen);
			this.HmacHash(tempKey, output2, output1, two);

			var output3 = output.Slice(2 * hashLen, hashLen);
			this.HmacHash(tempKey, output3, output2, three);
		}

		private void HmacHash(
			ReadOnlySpan<byte> key,
			Span<byte> hmac,
			ReadOnlySpan<byte> data1 = default,
			ReadOnlySpan<byte> data2 = default)
		{
			Debug.Assert(key.Length == this.inner.HashLen);
			Debug.Assert(hmac.Length == this.inner.HashLen);

			var blockLen = this.inner.BlockLen;

			Span<byte> ipad = stackalloc byte[blockLen];
			Span<byte> opad = stackalloc byte[blockLen];

			key.CopyTo(ipad);
			key.CopyTo(opad);

			for (int i = 0; i < blockLen; ++i)
			{
				ipad[i] ^= 0x36;
				opad[i] ^= 0x5C;
			}

			this.inner.AppendData(ipad);
			this.inner.AppendData(data1);
			this.inner.AppendData(data2);
			this.inner.GetHashAndReset(hmac);

			this.outer.AppendData(opad);
			this.outer.AppendData(hmac);
			this.outer.GetHashAndReset(hmac);
		}

		public void Dispose()
		{
			if (!this.disposed)
			{
				this.inner.Dispose();
				this.outer.Dispose();
				this.disposed = true;
			}
		}
	}
}
