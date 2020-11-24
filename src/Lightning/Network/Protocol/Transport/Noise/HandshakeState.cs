using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Network.Protocol.Transport.Noise
{
	internal sealed class HandshakeState<CipherType, DhType, HashType> : IHandshakeState
		where CipherType : Cipher, new()
		where DhType : Dh, new()
		where HashType : Hash, new()
	{
		private Dh dh = new DhType();
		private SymmetricState<CipherType, DhType, HashType> state;
		private Protocol protocol;
		private readonly Role role;
		private Role initiator;
		private bool turnToWrite;
		private KeyPair e;
		private KeyPair s;
		private byte[] re;
		private byte[] rs;
		private bool isPsk;
		private bool isOneWay;
		private readonly Queue<MessagePattern> messagePatterns = new Queue<MessagePattern>();
		private readonly Queue<byte[]> psks = new Queue<byte[]>();
		private bool disposed;
		private byte[] versionPrefix;

		public HandshakeState(
			Protocol protocol,
			bool initiator,
			ReadOnlySpan<byte> prologue,
			ReadOnlySpan<byte> s,
			ReadOnlySpan<byte> rs,
			IEnumerable<byte[]> psks,
			ReadOnlySpan<byte> versionPrefix)
		{
			Debug.Assert(psks != null);

			if (!s.IsEmpty && s.Length != this.dh.DhLen - 1) //TODO David fix to support different length for private and public keys
			{
				throw new ArgumentException("Invalid local static private key.", nameof(s));
			}
			
			if (!rs.IsEmpty && rs.Length != this.dh.DhLen)
			{
				throw new ArgumentException("Invalid remote static public key.", nameof(rs));
			}

			if (s.IsEmpty && protocol.HandshakePattern.LocalStaticRequired(initiator))
			{
				throw new ArgumentException("Local static private key required, but not provided.", nameof(s));
			}

			if (!s.IsEmpty && !protocol.HandshakePattern.LocalStaticRequired(initiator))
			{
				throw new ArgumentException("Local static private key provided, but not required.", nameof(s));
			}

			if (rs.IsEmpty && protocol.HandshakePattern.RemoteStaticRequired(initiator))
			{
				throw new ArgumentException("Remote static public key required, but not provided.", nameof(rs));
			}

			if (!rs.IsEmpty && !protocol.HandshakePattern.RemoteStaticRequired(initiator))
			{
				throw new ArgumentException("Remote static public key provided, but not required.", nameof(rs));
			}

			if ((protocol.Modifiers & PatternModifiers.Fallback) != 0)
			{
				throw new ArgumentException($"Fallback modifier can only be applied by calling the {nameof(this.Fallback)} method.");
			}

			this.state = new SymmetricState<CipherType, DhType, HashType>(protocol.Name);
			this.state.MixHash(prologue);

			this.protocol = protocol;
			this.role = initiator ? Role.Alice : Role.Bob;
			this.initiator = Role.Alice;
			this.turnToWrite = initiator;
			this.s = s.IsEmpty ? null : this.dh.GenerateKeyPair(s);
			this.rs = rs.IsEmpty ? null : rs.ToArray();

			this.ProcessPreMessages(protocol.HandshakePattern);
			this.ProcessPreSharedKeys(protocol, psks);

			var pskModifiers = PatternModifiers.Psk0 | PatternModifiers.Psk1 | PatternModifiers.Psk2 | PatternModifiers.Psk3;

			this.isPsk = (protocol.Modifiers & pskModifiers) != 0;
			this.isOneWay = this.messagePatterns.Count == 1;

			this.versionPrefix = versionPrefix.ToArray();
		}

		private void ProcessPreMessages(HandshakePattern handshakePattern)
		{
			foreach (var token in handshakePattern.Initiator.Tokens)
			{
				if (token == Token.S)
				{
					this.state.MixHash(this.role == Role.Alice ? this.s.PublicKey : this.rs);
				}
			}

			foreach (var token in handshakePattern.Responder.Tokens)
			{
				if (token == Token.S)
				{
					this.state.MixHash(this.role == Role.Alice ? this.rs : this.s.PublicKey);
				}
			}
		}

		private void ProcessPreSharedKeys(Protocol protocol, IEnumerable<byte[]> psks)
		{
			var patterns = protocol.HandshakePattern.Patterns;
			var modifiers = protocol.Modifiers;
			var position = 0;

			using (var enumerator = psks.GetEnumerator())
			{
				foreach (var pattern in patterns)
				{
					var modified = pattern;

					if (position == 0 && modifiers.HasFlag(PatternModifiers.Psk0))
					{
						modified = modified.PrependPsk();
						this.ProcessPreSharedKey(enumerator);
					}

					if (((int)modifiers & ((int)PatternModifiers.Psk1 << position)) != 0)
					{
						modified = modified.AppendPsk();
						this.ProcessPreSharedKey(enumerator);
					}

					this.messagePatterns.Enqueue(modified);
					++position;
				}

				if (enumerator.MoveNext())
				{
					throw new ArgumentException("Number of pre-shared keys was greater than the number of PSK modifiers.");
				}
			}
		}

		private void ProcessPreSharedKey(IEnumerator<byte[]> enumerator)
		{
			if (!enumerator.MoveNext())
			{
				throw new ArgumentException("Number of pre-shared keys was less than the number of PSK modifiers.");
			}

			var psk = enumerator.Current;

			if (psk.Length != Aead.KeySize)
			{
				throw new ArgumentException($"Pre-shared keys must be {Aead.KeySize} bytes in length.");
			}

			this.psks.Enqueue(psk.AsSpan().ToArray());
		}

		/// <summary>
		/// Overrides the DH function. It should only be used
		/// from Noise.Tests to fix the ephemeral private key.
		/// </summary>
		internal void SetDh(Dh dh)
		{
			this.dh = dh;
		}

		public ReadOnlySpan<byte> RemoteStaticPublicKey
		{
			get
			{
				this.ThrowIfDisposed();
				return this.rs;
			}
		}

		public void Fallback(Protocol protocol, ProtocolConfig config)
		{
			this.ThrowIfDisposed();
			Exceptions.ThrowIfNull(protocol, nameof(protocol));
			Exceptions.ThrowIfNull(config, nameof(config));

			if (config.LocalStatic == null)
			{
				throw new ArgumentException("Local static private key is required for the XXfallback pattern.");
			}

			if (this.initiator == Role.Bob)
			{
				throw new InvalidOperationException("Fallback cannot be applied to a Bob-initiated pattern.");
			}

			if (this.messagePatterns.Count + 1 != this.protocol.HandshakePattern.Patterns.Count())
			{
				throw new InvalidOperationException("Fallback can only be applied after the first handshake message.");
			}

			this.protocol = null;
			this.initiator = Role.Bob;
			this.turnToWrite = this.role == Role.Bob;

			this.s = this.dh.GenerateKeyPair(config.LocalStatic);
			this.rs = null;

			this.isPsk = false;
			this.isOneWay = false;

			while (this.psks.Count > 0)
			{
				var psk = this.psks.Dequeue();
				Utilities.ZeroMemory(psk);
			}

			this.state.Dispose();
			this.state = new SymmetricState<CipherType, DhType, HashType>(protocol.Name);
			this.state.MixHash(config.Prologue);

			if (this.role == Role.Alice)
			{
				Debug.Assert(this.e != null && this.re == null);
				this.state.MixHash(this.e.PublicKey);
			}
			else
			{
				Debug.Assert(this.e == null && this.re != null);
				this.state.MixHash(this.re);
			}

			this.messagePatterns.Clear();

			foreach (var pattern in protocol.HandshakePattern.Patterns.Skip(1))
			{
				this.messagePatterns.Enqueue(pattern);
			}
		}

		public (int, byte[], ITransport) WriteMessage(ReadOnlySpan<byte> payload, Span<byte> messageBuffer)
		{
			this.ThrowIfDisposed();

			if (this.messagePatterns.Count == 0)
			{
				throw new InvalidOperationException("Cannot call WriteMessage after the handshake has already been completed.");
			}

			var overhead = this.messagePatterns.Peek().Overhead(this.dh.DhLen, this.state.HasKey(), this.isPsk);
			var ciphertextSize = this.versionPrefix.Length + payload.Length + overhead;

			if (ciphertextSize > Protocol.MaxMessageLength)
			{
				throw new ArgumentException($"Noise message must be less than or equal to {Protocol.MaxMessageLength} bytes in length.");
			}

			if (ciphertextSize > messageBuffer.Length)
			{
				throw new ArgumentException("Message buffer does not have enough space to hold the ciphertext.");
			}

			if (!this.turnToWrite)
			{
				throw new InvalidOperationException("Unexpected call to WriteMessage (should be ReadMessage).");
			}

			var next = this.messagePatterns.Dequeue();
			var messageBufferLength = messageBuffer.Length;

			messageBuffer = this.WriteVersion(messageBuffer);
			
			foreach (var token in next.Tokens)
			{
				switch (token)
				{
					case Token.E: messageBuffer = this.WriteE(messageBuffer); break;
					case Token.S: messageBuffer = this.WriteS(messageBuffer); break;
					case Token.EE: this.DhAndMixKey(this.e, this.re); break;
					case Token.ES: this.ProcessES(); break;
					case Token.SE: this.ProcessSE(); break;
					case Token.SS: this.DhAndMixKey(this.s, this.rs); break;
					case Token.PSK: this.ProcessPSK(); break;
				}
			}

			int bytesWritten = this.state.EncryptAndHash(payload, messageBuffer);
			int size = messageBufferLength - messageBuffer.Length + bytesWritten;

			Debug.Assert(ciphertextSize == size);

			byte[] handshakeHash = this.state.GetHandshakeHash();
			ITransport transport = null;

			if (this.messagePatterns.Count == 0)
			{
				(handshakeHash, transport) = this.Split();
			}

			this.turnToWrite = false;
			return (ciphertextSize, handshakeHash, transport);
		}

		private Span<byte> WriteVersion(in Span<byte> messageBuffer)
		{
			this.versionPrefix.CopyTo(messageBuffer);
			return messageBuffer.Slice(this.versionPrefix.Length);
		}
		
		private ReadOnlySpan<byte> ReadVersionPrefix(in ReadOnlySpan<byte> message)
		{
			var messageVersion = message.Slice(0, this.versionPrefix.Length);
			
			Debug.Assert(messageVersion.SequenceEqual(this.versionPrefix));
			
			return message.Slice(this.versionPrefix.Length);
		}

		private Span<byte> WriteE(Span<byte> buffer)
		{
			Debug.Assert(this.e == null);

			this.e = this.dh.GenerateKeyPair();
			this.e.PublicKey.CopyTo(buffer);
			this.state.MixHash(this.e.PublicKey);

			if (this.isPsk)
			{
				this.state.MixKey(this.e.PublicKey);
			}

			return buffer.Slice(this.e.PublicKey.Length);
		}

		private Span<byte> WriteS(Span<byte> buffer)
		{
			Debug.Assert(this.s != null);

			var bytesWritten = this.state.EncryptAndHash(this.s.PublicKey, buffer);
			return buffer.Slice(bytesWritten);
		}

		public (int, byte[], ITransport) ReadMessage(ReadOnlySpan<byte> message, Span<byte> payloadBuffer)
		{
			this.ThrowIfDisposed();

			if (this.messagePatterns.Count == 0)
			{
				throw new InvalidOperationException("Cannot call WriteMessage after the handshake has already been completed.");
			}

			var overhead = this.messagePatterns.Peek().Overhead(this.dh.DhLen, this.state.HasKey(), this.isPsk);
			var plaintextSize = message.Length - overhead - this.versionPrefix.Length;

			if (message.Length > Protocol.MaxMessageLength)
			{
				throw new ArgumentException($"Noise message must be less than or equal to {Protocol.MaxMessageLength} bytes in length.");
			}

			if (message.Length < overhead)
			{
				throw new ArgumentException($"Noise message must be greater than or equal to {overhead} bytes in length.");
			}

			if (plaintextSize > payloadBuffer.Length)
			{
				throw new ArgumentException("Payload buffer does not have enough space to hold the plaintext.");
			}

			if (this.turnToWrite)
			{
				throw new InvalidOperationException("Unexpected call to ReadMessage (should be WriteMessage).");
			}

			var next = this.messagePatterns.Dequeue();
			var messageLength = message.Length;

			message = this.ReadVersionPrefix(message);
			
			foreach (var token in next.Tokens)
			{
				switch (token)
				{
					case Token.E: message = this.ReadE(message); break;
					case Token.S: message = this.ReadS(message); break;
					case Token.EE: this.DhAndMixKey(this.e, this.re); break;
					case Token.ES: this.ProcessES(); break;
					case Token.SE: this.ProcessSE(); break;
					case Token.SS: this.DhAndMixKey(this.s, this.rs); break;
					case Token.PSK: this.ProcessPSK(); break;
				}
			}

			int bytesRead = this.state.DecryptAndHash(message, payloadBuffer);
			Debug.Assert(bytesRead == plaintextSize);

			byte[] handshakeHash = this.state.GetHandshakeHash();
			ITransport transport = null;

			if (this.messagePatterns.Count == 0)
			{
				(handshakeHash, transport) = this.Split();
			}

			this.turnToWrite = true;
			return (plaintextSize, handshakeHash, transport);
		}

		private ReadOnlySpan<byte> ReadE(ReadOnlySpan<byte> buffer)
		{
			Debug.Assert(this.re == null);

			this.re = buffer.Slice(0, this.dh.DhLen).ToArray();
			this.state.MixHash(this.re);

			if (this.isPsk)
			{
				this.state.MixKey(this.re);
			}

			return buffer.Slice(this.re.Length);
		}

		private ReadOnlySpan<byte> ReadS(ReadOnlySpan<byte> message)
		{
			Debug.Assert(this.rs == null);

			var length = this.state.HasKey() ? this.dh.DhLen + Aead.TagSize : this.dh.DhLen;
			var temp = message.Slice(0, length);

			this.rs = new byte[this.dh.DhLen];
			this.state.DecryptAndHash(temp, this.rs);

			return message.Slice(length);
		}

		private void ProcessES()
		{
			if (this.role == Role.Alice)
			{
				this.DhAndMixKey(this.e, this.rs);
			}
			else
			{
				this.DhAndMixKey(this.s, this.re);
			}
		}

		private void ProcessSE()
		{
			if (this.role == Role.Alice)
			{
				this.DhAndMixKey(this.s, this.re);
			}
			else
			{
				this.DhAndMixKey(this.e, this.rs);
			}
		}

		private void ProcessPSK()
		{
			var psk = this.psks.Dequeue();
			this.state.MixKeyAndHash(psk);
			Utilities.ZeroMemory(psk);
		}

		private (byte[], ITransport) Split()
		{
			var (c1, c2) = this.state.Split();

			if (this.isOneWay)
			{
				c2.Dispose();
				c2 = null;
			}

			Debug.Assert(this.psks.Count == 0);

			var handshakeHash = this.state.GetHandshakeHash();
			var chainingKey = this.state.GetChainingKey();
			var transport = new Transport<CipherType>(this.role == this.initiator, c1, c2);

			this.Clear();

			return (handshakeHash, transport);
		}

		private void DhAndMixKey(KeyPair keyPair, ReadOnlySpan<byte> publicKey)
		{
			Debug.Assert(keyPair != null);
			Debug.Assert(!publicKey.IsEmpty);

			Span<byte> sharedKey = stackalloc byte[this.dh.DhLen - 1]; //TODO David add support for different length of private and public
			this.dh.Dh(keyPair, publicKey, sharedKey);
			this.state.MixKey(sharedKey);
		}

		private void Clear()
		{
			this.state.Dispose();
			this.e?.Dispose();
			this.s?.Dispose();

			foreach (var psk in this.psks)
			{
				Utilities.ZeroMemory(psk);
			}
		}

		private void ThrowIfDisposed()
		{
			Exceptions.ThrowIfDisposed(this.disposed, nameof(HandshakeState<CipherType, DhType, HashType>));
		}

		public void Dispose()
		{
			if (!this.disposed)
			{
				this.Clear();
				this.disposed = true;
			}
		}

		private enum Role
		{
			Alice,
			Bob
		}
	}
}
