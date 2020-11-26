using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Network.Protocol.Transport.Noise
{
   internal sealed class HandshakeState<TCipherType, TDhType, THashType> : IHandshakeState
      where TCipherType : ICipher, new()
      where TDhType : IDh, new()
      where THashType : IHash, new()
   {
      private IDh _dh = new TDhType();
      private SymmetricState<TCipherType, TDhType, THashType> _state;
      private Protocol _protocol;
      private readonly Role _role;
      private Role _initiator;
      private bool _turnToWrite;
      private KeyPair _e;
      private KeyPair _s;
      private byte[] _re;
      private byte[] _rs;
      private bool _isPsk;
      private bool _isOneWay;
      private readonly Queue<MessagePattern> _messagePatterns = new Queue<MessagePattern>();
      private readonly Queue<byte[]> _psks = new Queue<byte[]>();
      private bool _disposed;
      private byte[] _versionPrefix;

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

         if (!s.IsEmpty && s.Length != _dh.DhLen - 1) //TODO David fix to support different length for private and public keys
         {
            throw new ArgumentException("Invalid local static private key.", nameof(s));
         }

         if (!rs.IsEmpty && rs.Length != _dh.DhLen)
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
            throw new ArgumentException($"Fallback modifier can only be applied by calling the {nameof(Fallback)} method.");
         }

         _state = new SymmetricState<TCipherType, TDhType, THashType>(protocol.Name);
         _state.MixHash(prologue);

         _protocol = protocol;
         _role = initiator ? Role.Alice : Role.Bob;
         _initiator = Role.Alice;
         _turnToWrite = initiator;
         _s = s.IsEmpty ? null : _dh.GenerateKeyPair(s);
         _rs = rs.IsEmpty ? null : rs.ToArray();

         ProcessPreMessages(protocol.HandshakePattern);
         ProcessPreSharedKeys(protocol, psks);

         var pskModifiers = PatternModifiers.Psk0 | PatternModifiers.Psk1 | PatternModifiers.Psk2 | PatternModifiers.Psk3;

         _isPsk = (protocol.Modifiers & pskModifiers) != 0;
         _isOneWay = _messagePatterns.Count == 1;

         _versionPrefix = versionPrefix.ToArray();
      }

      private void ProcessPreMessages(HandshakePattern handshakePattern)
      {
         foreach (var token in handshakePattern.Initiator.Tokens)
         {
            if (token == Token.S)
            {
               _state.MixHash(_role == Role.Alice ? _s.PublicKey : _rs);
            }
         }

         foreach (var token in handshakePattern.Responder.Tokens)
         {
            if (token == Token.S)
            {
               _state.MixHash(_role == Role.Alice ? _rs : _s.PublicKey);
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
                  ProcessPreSharedKey(enumerator);
               }

               if (((int)modifiers & ((int)PatternModifiers.Psk1 << position)) != 0)
               {
                  modified = modified.AppendPsk();
                  ProcessPreSharedKey(enumerator);
               }

               _messagePatterns.Enqueue(modified);
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

         if (psk.Length != Aead.KEY_SIZE)
         {
            throw new ArgumentException($"Pre-shared keys must be {Aead.KEY_SIZE} bytes in length.");
         }

         _psks.Enqueue(psk.AsSpan().ToArray());
      }

      /// <summary>
      /// Overrides the DH function. It should only be used
      /// from Noise.Tests to fix the ephemeral private key.
      /// </summary>
      internal void SetDh(IDh dh)
      {
         _dh = dh;
      }

      public ReadOnlySpan<byte> RemoteStaticPublicKey
      {
         get
         {
            ThrowIfDisposed();
            return _rs;
         }
      }

      public void Fallback(Protocol protocol, ProtocolConfig config)
      {
         ThrowIfDisposed();
         Exceptions.ThrowIfNull(protocol, nameof(protocol));
         Exceptions.ThrowIfNull(config, nameof(config));

         if (config.LocalStatic == null)
         {
            throw new ArgumentException("Local static private key is required for the XXfallback pattern.");
         }

         if (_initiator == Role.Bob)
         {
            throw new InvalidOperationException("Fallback cannot be applied to a Bob-initiated pattern.");
         }

         if (_messagePatterns.Count + 1 != _protocol.HandshakePattern.Patterns.Count())
         {
            throw new InvalidOperationException("Fallback can only be applied after the first handshake message.");
         }

         _protocol = null;
         _initiator = Role.Bob;
         _turnToWrite = _role == Role.Bob;

         _s = _dh.GenerateKeyPair(config.LocalStatic);
         _rs = null;

         _isPsk = false;
         _isOneWay = false;

         while (_psks.Count > 0)
         {
            var psk = _psks.Dequeue();
            Utilities.ZeroMemory(psk);
         }

         _state.Dispose();
         _state = new SymmetricState<TCipherType, TDhType, THashType>(protocol.Name);
         _state.MixHash(config.Prologue);

         if (_role == Role.Alice)
         {
            Debug.Assert(_e != null && _re == null);
            _state.MixHash(_e.PublicKey);
         }
         else
         {
            Debug.Assert(_e == null && _re != null);
            _state.MixHash(_re);
         }

         _messagePatterns.Clear();

         foreach (var pattern in protocol.HandshakePattern.Patterns.Skip(1))
         {
            _messagePatterns.Enqueue(pattern);
         }
      }

      public (int, byte[], ITransport) WriteMessage(ReadOnlySpan<byte> payload, Span<byte> messageBuffer)
      {
         ThrowIfDisposed();

         if (_messagePatterns.Count == 0)
         {
            throw new InvalidOperationException("Cannot call WriteMessage after the handshake has already been completed.");
         }

         var overhead = _messagePatterns.Peek().Overhead(_dh.DhLen, _state.HasKey(), _isPsk);
         var ciphertextSize = _versionPrefix.Length + payload.Length + overhead;

         if (ciphertextSize > Protocol.MAX_MESSAGE_LENGTH)
         {
            throw new ArgumentException($"Noise message must be less than or equal to {Protocol.MAX_MESSAGE_LENGTH} bytes in length.");
         }

         if (ciphertextSize > messageBuffer.Length)
         {
            throw new ArgumentException("Message buffer does not have enough space to hold the ciphertext.");
         }

         if (!_turnToWrite)
         {
            throw new InvalidOperationException("Unexpected call to WriteMessage (should be ReadMessage).");
         }

         var next = _messagePatterns.Dequeue();
         var messageBufferLength = messageBuffer.Length;

         messageBuffer = WriteVersion(messageBuffer);

         foreach (var token in next.Tokens)
         {
            switch (token)
            {
               case Token.E: messageBuffer = WriteE(messageBuffer); break;
               case Token.S: messageBuffer = WriteS(messageBuffer); break;
               case Token.Ee: DhAndMixKey(_e, _re); break;
               case Token.Es: ProcessEs(); break;
               case Token.Se: ProcessSe(); break;
               case Token.Ss: DhAndMixKey(_s, _rs); break;
               case Token.Psk: ProcessPsk(); break;
            }
         }

         int bytesWritten = _state.EncryptAndHash(payload, messageBuffer);
         int size = messageBufferLength - messageBuffer.Length + bytesWritten;

         Debug.Assert(ciphertextSize == size);

         byte[] handshakeHash = _state.GetHandshakeHash();
         ITransport transport = null;

         if (_messagePatterns.Count == 0)
         {
            (handshakeHash, transport) = Split();
         }

         _turnToWrite = false;
         return (ciphertextSize, handshakeHash, transport);
      }

      private Span<byte> WriteVersion(in Span<byte> messageBuffer)
      {
         _versionPrefix.CopyTo(messageBuffer);
         return messageBuffer.Slice(_versionPrefix.Length);
      }

      private ReadOnlySpan<byte> ReadVersionPrefix(in ReadOnlySpan<byte> message)
      {
         var messageVersion = message.Slice(0, _versionPrefix.Length);

         Debug.Assert(messageVersion.SequenceEqual(_versionPrefix));

         return message.Slice(_versionPrefix.Length);
      }

      private Span<byte> WriteE(Span<byte> buffer)
      {
         Debug.Assert(_e == null);

         _e = _dh.GenerateKeyPair();
         _e.PublicKey.CopyTo(buffer);
         _state.MixHash(_e.PublicKey);

         if (_isPsk)
         {
            _state.MixKey(_e.PublicKey);
         }

         return buffer.Slice(_e.PublicKey.Length);
      }

      private Span<byte> WriteS(Span<byte> buffer)
      {
         Debug.Assert(_s != null);

         var bytesWritten = _state.EncryptAndHash(_s.PublicKey, buffer);
         return buffer.Slice(bytesWritten);
      }

      public (int, byte[], ITransport) ReadMessage(ReadOnlySpan<byte> message, Span<byte> payloadBuffer)
      {
         ThrowIfDisposed();

         if (_messagePatterns.Count == 0)
         {
            throw new InvalidOperationException("Cannot call WriteMessage after the handshake has already been completed.");
         }

         var overhead = _messagePatterns.Peek().Overhead(_dh.DhLen, _state.HasKey(), _isPsk);
         var plaintextSize = message.Length - overhead - _versionPrefix.Length;

         if (message.Length > Protocol.MAX_MESSAGE_LENGTH)
         {
            throw new ArgumentException($"Noise message must be less than or equal to {Protocol.MAX_MESSAGE_LENGTH} bytes in length.");
         }

         if (message.Length < overhead)
         {
            throw new ArgumentException($"Noise message must be greater than or equal to {overhead} bytes in length.");
         }

         if (plaintextSize > payloadBuffer.Length)
         {
            throw new ArgumentException("Payload buffer does not have enough space to hold the plaintext.");
         }

         if (_turnToWrite)
         {
            throw new InvalidOperationException("Unexpected call to ReadMessage (should be WriteMessage).");
         }

         var next = _messagePatterns.Dequeue();
         var messageLength = message.Length;

         message = ReadVersionPrefix(message);

         foreach (var token in next.Tokens)
         {
            switch (token)
            {
               case Token.E: message = ReadE(message); break;
               case Token.S: message = ReadS(message); break;
               case Token.Ee: DhAndMixKey(_e, _re); break;
               case Token.Es: ProcessEs(); break;
               case Token.Se: ProcessSe(); break;
               case Token.Ss: DhAndMixKey(_s, _rs); break;
               case Token.Psk: ProcessPsk(); break;
            }
         }

         int bytesRead = _state.DecryptAndHash(message, payloadBuffer);
         Debug.Assert(bytesRead == plaintextSize);

         byte[] handshakeHash = _state.GetHandshakeHash();
         ITransport transport = null;

         if (_messagePatterns.Count == 0)
         {
            (handshakeHash, transport) = Split();
         }

         _turnToWrite = true;
         return (plaintextSize, handshakeHash, transport);
      }

      private ReadOnlySpan<byte> ReadE(ReadOnlySpan<byte> buffer)
      {
         Debug.Assert(_re == null);

         _re = buffer.Slice(0, _dh.DhLen).ToArray();
         _state.MixHash(_re);

         if (_isPsk)
         {
            _state.MixKey(_re);
         }

         return buffer.Slice(_re.Length);
      }

      private ReadOnlySpan<byte> ReadS(ReadOnlySpan<byte> message)
      {
         Debug.Assert(_rs == null);

         var length = _state.HasKey() ? _dh.DhLen + Aead.TAG_SIZE : _dh.DhLen;
         var temp = message.Slice(0, length);

         _rs = new byte[_dh.DhLen];
         _state.DecryptAndHash(temp, _rs);

         return message.Slice(length);
      }

      private void ProcessEs()
      {
         if (_role == Role.Alice)
         {
            DhAndMixKey(_e, _rs);
         }
         else
         {
            DhAndMixKey(_s, _re);
         }
      }

      private void ProcessSe()
      {
         if (_role == Role.Alice)
         {
            DhAndMixKey(_s, _re);
         }
         else
         {
            DhAndMixKey(_e, _rs);
         }
      }

      private void ProcessPsk()
      {
         var psk = _psks.Dequeue();
         _state.MixKeyAndHash(psk);
         Utilities.ZeroMemory(psk);
      }

      private (byte[], ITransport) Split()
      {
         var (c1, c2) = _state.Split();

         if (_isOneWay)
         {
            c2.Dispose();
            c2 = null;
         }

         Debug.Assert(_psks.Count == 0);

         var handshakeHash = _state.GetHandshakeHash();
         var chainingKey = _state.GetChainingKey();
         var transport = new Transport<TCipherType>(_role == _initiator, c1, c2);

         Clear();

         return (handshakeHash, transport);
      }

      private void DhAndMixKey(KeyPair keyPair, ReadOnlySpan<byte> publicKey)
      {
         Debug.Assert(keyPair != null);
         Debug.Assert(!publicKey.IsEmpty);

         Span<byte> sharedKey = stackalloc byte[_dh.DhLen - 1]; //TODO David add support for different length of private and public
         _dh.Dh(keyPair, publicKey, sharedKey);
         _state.MixKey(sharedKey);
      }

      private void Clear()
      {
         _state.Dispose();
         _e?.Dispose();
         _s?.Dispose();

         foreach (var psk in _psks)
         {
            Utilities.ZeroMemory(psk);
         }
      }

      private void ThrowIfDisposed()
      {
         Exceptions.ThrowIfDisposed(_disposed, nameof(HandshakeState<TCipherType, TDhType, THashType>));
      }

      public void Dispose()
      {
         if (!_disposed)
         {
            Clear();
            _disposed = true;
         }
      }

      private enum Role
      {
         Alice,
         Bob
      }
   }
}