using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Network.Protocol.Transport.Noise
{
	/// <summary>
	/// SHA-256 from System.Security.Cryptography.
	/// </summary>
	internal sealed class Sha256 : Hash
	{
		private readonly byte[] _state = new byte[104];
		private int _currentStateLength = 0;
		private bool _disposed;

		public Sha256() => this.Reset();

		public int HashLen => 32;
		public int BlockLen => 64;

		public void AppendData(ReadOnlySpan<byte> data)
		{
			if (data.IsEmpty) return;

			data.CopyTo(this._state.AsSpan(this._currentStateLength,data.Length));

			this._currentStateLength += data.Length;
		}

		public void GetHashAndReset(Span<byte> hash)
		{
			Debug.Assert(hash.Length == this.HashLen);

			using (var sha256 = SHA256.Create())
			{
				sha256.ComputeHash(this._state.AsSpan(0,this._currentStateLength)
					.ToArray());
				sha256.Hash.AsSpan()
					.CopyTo(hash);
			}

			this.Reset();
		}

		private void Reset()
		{
			this._currentStateLength = 0;
		}

		public void Dispose()
		{
			if (!this._disposed)
			{
				this._disposed = true;
			}
		}
	}
}
