using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Network.Protocol.Transport.Noise
{
	/// <summary>
	/// A concrete Noise protocol (e.g. Noise_XX_25519_AESGCM_SHA256 or Noise_IK_25519_ChaChaPoly_BLAKE2b).
	/// </summary>
	public sealed class Protocol
	{
		/// <summary>
		/// Maximum size of the Noise protocol message in bytes.
		/// </summary>
		public const int MaxMessageLength = 65535;

		/// <summary>
		/// Minimum size of the protocol name in bytes.
		/// </summary>
		private static readonly int MinProtocolNameLength = "Noise_N_448_AESGCM_SHA256".Length;

		/// <summary>
		/// Maximum size of the protocol name in bytes.
		/// </summary>
		private const int MaxProtocolNameLength = 255;

		private static readonly Dictionary<string, HandshakePattern> patterns = typeof(HandshakePattern).GetTypeInfo().DeclaredFields
			.Where(field => field.IsPublic && field.IsStatic && field.FieldType == typeof(HandshakePattern))
			.ToDictionary(field => field.Name, field => (HandshakePattern)field.GetValue(null));

		/// <summary>
		/// Allow first bytes of message in buffer as a version (to fit lightning protocol)
		/// </summary>
		public byte[] VersionPrefix { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="Protocol"/>
		/// class using ChaChaPoly, 25519, and SHA256 functions.
		/// </summary>
		/// <param name="handshakePattern">The handshake pattern (e.q. NX or IK).</param>
		/// <param name="modifiers">The combination of pattern modifiers (e.q. empty, psk0, or psk1+psk2).</param>
		/// <exception cref="ArgumentNullException">
		/// Thrown if the <paramref name="handshakePattern"/> is null.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Thrown if <paramref name="modifiers"/> does not represent a valid combination of pattern modifiers.
		/// </exception>
		public Protocol(HandshakePattern handshakePattern, PatternModifiers modifiers = PatternModifiers.None)
			: this(handshakePattern, CipherFunction.ChaChaPoly, HashFunction.Sha256, modifiers)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Protocol"/> class.
		/// </summary>
		/// <param name="handshakePattern">The handshake pattern (e.q. NX or IK).</param>
		/// <param name="cipher">The cipher function (AESGCM or ChaChaPoly).</param>
		/// <param name="hash">The hash function (SHA256, SHA512, BLAKE2s, or BLAKE2b).</param>
		/// <param name="modifiers">The combination of pattern modifiers (e.q. empty, psk0, or psk1+psk2).</param>
		/// <exception cref="ArgumentNullException">
		/// Thrown if either <paramref name="handshakePattern"/>,
		/// <paramref name="cipher"/>, or <paramref name="hash"/> is null.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Thrown if <paramref name="modifiers"/> does not represent a valid combination of pattern modifiers.
		/// </exception>
		public Protocol(
			HandshakePattern handshakePattern,
			CipherFunction cipher,
			HashFunction hash,
			PatternModifiers modifiers = PatternModifiers.None)
		{
			Exceptions.ThrowIfNull(handshakePattern, nameof(handshakePattern));
			Exceptions.ThrowIfNull(cipher, nameof(cipher));
			Exceptions.ThrowIfNull(hash, nameof(hash));

			this.HandshakePattern = handshakePattern;
			this.Cipher = cipher;
			this.Dh = DhFunction.CurveSecp256K1;
			this.Hash = hash;
			this.Modifiers = modifiers;

			this.Name = this.GetName();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Protocol"/> class.
		/// </summary>
		/// <param name="handshakePattern">The handshake pattern (e.q. NX or IK).</param>
		/// <param name="cipher">The cipher function (AESGCM or ChaChaPoly).</param>
		/// <param name="hash">The hash function (SHA256, SHA512, BLAKE2s, or BLAKE2b).</param>
		/// <param name="curveName"></param>
		/// <param name="versionPrefix">Version required as part for first byte</param>
		/// <param name="modifiers">The combination of pattern modifiers (e.q. empty, psk0, or psk1+psk2).</param>
		/// <exception cref="ArgumentNullException">
		/// Thrown if either <paramref name="handshakePattern"/>,
		/// <paramref name="cipher"/>, or <paramref name="hash"/> is null.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Thrown if <paramref name="modifiers"/> does not represent a valid combination of pattern modifiers.
		/// </exception>
		public Protocol(
			HandshakePattern handshakePattern,
			CipherFunction cipher,
			HashFunction hash,
			ReadOnlySpan<char> curveName,
			ReadOnlySpan<byte> versionPrefix,
			PatternModifiers modifiers = PatternModifiers.None)
		{
			Exceptions.ThrowIfNull(handshakePattern, nameof(handshakePattern));
			Exceptions.ThrowIfNull(cipher, nameof(cipher));
			Exceptions.ThrowIfNull(hash, nameof(hash));

			this.HandshakePattern = handshakePattern;
			this.Cipher = cipher;
			this.Dh = DhFunction.Parse(curveName);
			this.Hash = hash;
			this.Modifiers = modifiers;

			versionPrefix.CopyTo(this.VersionPrefix);
			
			this.Name = this.GetName();
		}

		/// <summary>
		/// Gets the handshake pattern.
		/// </summary>
		public HandshakePattern HandshakePattern { get; }

		/// <summary>
		/// Gets the cipher function.
		/// </summary>
		public CipherFunction Cipher { get; }

		/// <summary
		/// >Gets the Diffie-Hellman function.
		/// </summary>
		public DhFunction Dh { get; }

		/// <summary>
		/// Gets the hash function.
		/// </summary>
		public HashFunction Hash { get; }

		/// <summary>
		/// Gets the combination of pattern modifiers.
		/// </summary>
		public PatternModifiers Modifiers { get; }

		internal byte[] Name { get; }

		/// <summary>
		/// Creates an initial <see cref="IHandshakeState"/>.
		/// </summary>
		/// <param name="initiator">A boolean indicating the initiator or responder role.</param>
		/// <param name="prologue">
		/// A byte sequence which may be zero-length, or which may contain
		/// context information that both parties want to confirm is identical.
		/// </param>
		/// <param name="s">The local static private key (optional).</param>
		/// <param name="rs">The remote party's static public key (optional).</param>
		/// <param name="psks">The collection of zero or more 32-byte pre-shared secret keys.</param>
		/// <returns>The initial handshake state.</returns>
		/// <exception cref="ArgumentException">
		/// Thrown if any of the following conditions is satisfied:
		/// <para>- <paramref name="s"/> is not a valid DH private key.</para>
		/// <para>- <paramref name="rs"/> is not a valid DH public key.</para>
		/// <para>- <see cref="HandshakePattern"/> requires the <see cref="IHandshakeState"/>
		/// to be initialized with local and/or remote static key,
		/// but <paramref name="s"/> and/or <paramref name="rs"/> is null.</para>
		/// <para>- One or more pre-shared keys are not 32 bytes in length.</para>
		/// <para>- Number of pre-shared keys does not match the number of PSK modifiers.</para>
		/// <para>- Fallback modifier is present (fallback can only be applied by calling
		/// the <see cref="IHandshakeState.Fallback"/> method on existing handshake state).</para>
		/// </exception>
		public IHandshakeState Create(
			bool initiator,
			ReadOnlySpan<byte> prologue = default,
			byte[] s = default,
			byte[] rs = default,
			IEnumerable<byte[]> psks = default)
		{
			if (psks == null)
			{
				psks = Enumerable.Empty<byte[]>();
			}

			
			if (this.Cipher == CipherFunction.ChaChaPoly && this.Hash == HashFunction.Sha256)
			{
				return new HandshakeState<ChaCha20Poly1305, CurveSecp256K1, Sha256>(this, initiator, prologue, s, rs,
					psks, this.VersionPrefix);
			}

			throw new InvalidOperationException();
		}

		/// <summary>
		/// Creates an initial <see cref="IHandshakeState"/>.
		/// </summary>
		/// <param name="config">
		/// A set of parameters used to instantiate an
		/// initial <see cref="IHandshakeState"/>.
		/// </param>
		/// <returns>The initial handshake state.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown if the <paramref name="config"/> is null.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Thrown if any of the following conditions is satisfied:
		/// <para>- <paramref name="config"/> does not contain a valid DH private key.</para>
		/// <para>- <paramref name="config"/> does not contain a valid DH public key.</para>
		/// <para>- <see cref="HandshakePattern"/> requires the <see cref="IHandshakeState"/>
		/// to be initialized with local and/or remote static key,
		/// but <see cref="ProtocolConfig.LocalStatic"/> and/or
		/// <see cref="ProtocolConfig.RemoteStatic"/> is null.</para>
		/// <para>- One or more pre-shared keys are not 32 bytes in length.</para>
		/// <para>- Number of pre-shared keys does not match the number of PSK modifiers.</para>
		/// </exception>
		public IHandshakeState Create(ProtocolConfig config)
		{
			Exceptions.ThrowIfNull(config, nameof(config));

			return this.Create(config.Initiator, config.Prologue, config.LocalStatic, config.RemoteStatic, config.PreSharedKeys);
		}

		/// <summary>
		/// Converts the Noise protocol name to its <see cref="Protocol"/> equivalent.
		/// </summary>
		/// <param name="s">The Noise protocol name (e.q. Noise_KNpsk2_25519_ChaChaPoly_SHA512).</param>
		/// <returns>
		/// An object that is equivalent to the Noise
		/// protocol name contained in <paramref name="s"/>.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown if <paramref name="s"/> is not a valid Noise protocol name.
		/// </exception>
		public static Protocol Parse(ReadOnlySpan<char> s)
		{
			if (s.Length < MinProtocolNameLength || s.Length > MaxProtocolNameLength)
			{
				throw new ArgumentException("Invalid Noise protocol name.", nameof(s));
			}

			var splitter = new StringSplitter(s, '_');
			var noise = splitter.Next();

			if (!noise.SequenceEqual("Noise".AsSpan()))
			{
				throw new ArgumentException("Invalid Noise protocol name.", nameof(s));
			}

			var next = splitter.Next();
			var pattern = next.Length > 1 && Char.IsUpper(next[1]) ? next.Slice(0, 2) : next.Slice(0, 1);

			var handshakePattern = ParseHandshakePattern(pattern);
			var modifiers = ParseModifiers(next.Slice(pattern.Length));

			var dh = DhFunction.Parse(splitter.Next());
			Debug.Assert(dh == DhFunction.CurveSecp256K1);

			var cipher = CipherFunction.Parse(splitter.Next());
			var hash = HashFunction.Parse(splitter.Next());

			if (!splitter.Next().IsEmpty)
			{
				throw new ArgumentException("Invalid Noise protocol name.", nameof(s));
			}

			var protocol = new Protocol(handshakePattern, cipher, hash, modifiers);
			ValidateProtocolName(s, protocol);

			return protocol;
		}

		[Conditional("DEBUG")]
		private static void ValidateProtocolName(ReadOnlySpan<char> s, Protocol protocol)
		{
			var actual = Encoding.ASCII.GetString(protocol.Name);
			var expected = new string(s.ToArray());

			Debug.Assert(actual == expected);
		}

		private static HandshakePattern ParseHandshakePattern(ReadOnlySpan<char> s)
		{
			foreach (var pattern in patterns)
			{
				if (pattern.Key.AsSpan().SequenceEqual(s))
				{
					return pattern.Value;
				}
			}

			throw new ArgumentException("Invalid Noise handshake pattern name.", nameof(s));
		}

		private static PatternModifiers ParseModifiers(ReadOnlySpan<char> s)
		{
			var splitter = new StringSplitter(s, '+');
			var modifiers = PatternModifiers.None;

			for (var next = splitter.Next(); !next.IsEmpty; next = splitter.Next())
			{
				var modifier = ParseModifier(next);

				if (modifier <= modifiers)
				{
					throw new ArgumentException("PSK pattern modifiers are required to be sorted alphabetically.");
				}

				modifiers |= modifier;
			}

			return modifiers;
		}

		private static PatternModifiers ParseModifier(ReadOnlySpan<char> s)
		{
			switch (s)
			{
				case var _ when s.SequenceEqual("psk0".AsSpan()): return PatternModifiers.Psk0;
				case var _ when s.SequenceEqual("psk1".AsSpan()): return PatternModifiers.Psk1;
				case var _ when s.SequenceEqual("psk2".AsSpan()): return PatternModifiers.Psk2;
				case var _ when s.SequenceEqual("psk3".AsSpan()): return PatternModifiers.Psk3;
				case var _ when s.SequenceEqual("fallback".AsSpan()): return PatternModifiers.Fallback;
				default: throw new ArgumentException("Unknown pattern modifier.", nameof(s));
			}
		}

		private byte[] GetName()
		{
			var protocolName = new StringBuilder("Noise");

			protocolName.Append('_');
			protocolName.Append(this.HandshakePattern.Name);

			if (this.Modifiers != PatternModifiers.None)
			{
				var separator = String.Empty;

				foreach (PatternModifiers modifier in Enum.GetValues(typeof(PatternModifiers)))
				{
					if ((this.Modifiers & modifier) != PatternModifiers.None)
					{
						protocolName.Append(separator);
						protocolName.Append(modifier.ToString().ToLowerInvariant());
						separator = "+";
					}
				}
			}

			protocolName.Append('_');
			protocolName.Append(this.Dh);

			protocolName.Append('_');
			protocolName.Append(this.Cipher);

			protocolName.Append('_');
			protocolName.Append(this.Hash);

			Debug.Assert(protocolName.Length <= MaxProtocolNameLength);

			return Encoding.ASCII.GetBytes(protocolName.ToString());
		}

		private ref struct StringSplitter
		{
			private ReadOnlySpan<char> s;
			private char separator;

			public StringSplitter(ReadOnlySpan<char> s, char separator)
			{
				this.s = s;
				this.separator = separator;
			}

			public ReadOnlySpan<char> Next()
			{
				int index = this.s.IndexOf(this.separator);

				if (index > 0)
				{
					var next = this.s.Slice(0, index);
					this.s = this.s.Slice(index + 1);

					return next;
				}
				else
				{
					var next = this.s;
					this.s = ReadOnlySpan<char>.Empty;

					return next;
				}
			}
		}
	}
}
