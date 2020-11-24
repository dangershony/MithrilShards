using System;
using System.Diagnostics;

namespace Network.Protocol.Transport.Noise
{
	internal sealed class Transport<CipherType> : ITransport where CipherType : Cipher, new()
	{
		private readonly bool initiator;
		private readonly CipherState<CipherType> c1;
		private readonly CipherState<CipherType> c2;
		private bool disposed;

		public Transport(bool initiator, CipherState<CipherType> c1, CipherState<CipherType> c2)
		{
			Exceptions.ThrowIfNull(c1, nameof(c1));

			this.initiator = initiator;
			this.c1 = c1;
			this.c2 = c2;
		}

		public bool IsOneWay
		{
			get
			{
				Exceptions.ThrowIfDisposed(this.disposed, nameof(Transport<CipherType>));
				return this.c2 == null;
			}
		}

		public int WriteMessage(ReadOnlySpan<byte> payload, Span<byte> messageBuffer)
		{
			Exceptions.ThrowIfDisposed(this.disposed, nameof(Transport<CipherType>));

			if (!this.initiator && this.IsOneWay)
			{
				throw new InvalidOperationException("Responder cannot write messages to a one-way stream.");
			}

			if (payload.Length + Aead.TagSize > Protocol.MaxMessageLength)
			{
				throw new ArgumentException($"Noise message must be less than or equal to {Protocol.MaxMessageLength} bytes in length.");
			}

			if (payload.Length + Aead.TagSize > messageBuffer.Length)
			{
				throw new ArgumentException("Message buffer does not have enough space to hold the ciphertext.");
			}

			var cipher = this.initiator ? this.c1 : this.c2;
			Debug.Assert(cipher.HasKey());

			return cipher.EncryptWithAd(null, payload, messageBuffer);
		}

		public int ReadMessage(ReadOnlySpan<byte> message, Span<byte> payloadBuffer)
		{
			Exceptions.ThrowIfDisposed(this.disposed, nameof(Transport<CipherType>));

			if (this.initiator && this.IsOneWay)
			{
				throw new InvalidOperationException("Initiator cannot read messages from a one-way stream.");
			}

			if (message.Length > Protocol.MaxMessageLength)
			{
				throw new ArgumentException($"Noise message must be less than or equal to {Protocol.MaxMessageLength} bytes in length.");
			}

			if (message.Length < Aead.TagSize)
			{
				throw new ArgumentException($"Noise message must be greater than or equal to {Aead.TagSize} bytes in length.");
			}

			if (message.Length - Aead.TagSize > payloadBuffer.Length)
			{
				throw new ArgumentException("Payload buffer does not have enough space to hold the plaintext.");
			}

			var cipher = this.initiator ? this.c2 : this.c1;
			Debug.Assert(cipher.HasKey());

			return cipher.DecryptWithAd(null, message, payloadBuffer);
		}

		public ulong GetNumberOfInitiatorMessages()
		{
			return this.c1.GetNonce();
		}

		public ulong GetNumberOfResponderMessages()
		{
			return this.c2.GetNonce();
		}

		public void KeyRecycleInitiatorToResponder()
		{
			Exceptions.ThrowIfDisposed(this.disposed, nameof(Transport<CipherType>));

			this.c1.KeyRecycle();
		}

		public void KeyRecycleResponderToInitiator()
		{
			Exceptions.ThrowIfDisposed(this.disposed, nameof(Transport<CipherType>));

			if (this.IsOneWay)
			{
				throw new InvalidOperationException("Cannot rekey responder to initiator in a one-way stream.");
			}

			this.c2.KeyRecycle();
		}

		public void Dispose()
		{
			if (!this.disposed)
			{
				this.c1.Dispose();
				this.c2?.Dispose();
				this.disposed = true;
			}
		}
	}
}
