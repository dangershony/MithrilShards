using System;
using System.Diagnostics;

namespace Network.Protocol.Transport.Noise
{
   internal sealed class Transport<TCipherType> : ITransport where TCipherType : ICipher, new()
   {
      private readonly bool _initiator;
      private readonly CipherState<TCipherType> _c1;
      private readonly CipherState<TCipherType> _c2;
      private bool _disposed;

      public Transport(bool initiator, CipherState<TCipherType> c1, CipherState<TCipherType> c2)
      {
         Exceptions.ThrowIfNull(c1, nameof(c1));

         _initiator = initiator;
         _c1 = c1;
         _c2 = c2;
      }

      public bool IsOneWay
      {
         get
         {
            Exceptions.ThrowIfDisposed(_disposed, nameof(Transport<TCipherType>));
            return _c2 == null;
         }
      }

      public int WriteMessage(ReadOnlySpan<byte> payload, Span<byte> messageBuffer)
      {
         Exceptions.ThrowIfDisposed(_disposed, nameof(Transport<TCipherType>));

         if (!_initiator && IsOneWay)
         {
            throw new InvalidOperationException("Responder cannot write messages to a one-way stream.");
         }

         if (payload.Length + Aead.TAG_SIZE > Protocol.MAX_MESSAGE_LENGTH)
         {
            throw new ArgumentException($"Noise message must be less than or equal to {Protocol.MAX_MESSAGE_LENGTH} bytes in length.");
         }

         if (payload.Length + Aead.TAG_SIZE > messageBuffer.Length)
         {
            throw new ArgumentException("Message buffer does not have enough space to hold the ciphertext.");
         }

         var cipher = _initiator ? _c1 : _c2;
         Debug.Assert(cipher.HasKey());

         return cipher.EncryptWithAd(null, payload, messageBuffer);
      }

      public int ReadMessage(ReadOnlySpan<byte> message, Span<byte> payloadBuffer)
      {
         Exceptions.ThrowIfDisposed(_disposed, nameof(Transport<TCipherType>));

         if (_initiator && IsOneWay)
         {
            throw new InvalidOperationException("Initiator cannot read messages from a one-way stream.");
         }

         if (message.Length > Protocol.MAX_MESSAGE_LENGTH)
         {
            throw new ArgumentException($"Noise message must be less than or equal to {Protocol.MAX_MESSAGE_LENGTH} bytes in length.");
         }

         if (message.Length < Aead.TAG_SIZE)
         {
            throw new ArgumentException($"Noise message must be greater than or equal to {Aead.TAG_SIZE} bytes in length.");
         }

         if (message.Length - Aead.TAG_SIZE > payloadBuffer.Length)
         {
            throw new ArgumentException("Payload buffer does not have enough space to hold the plaintext.");
         }

         var cipher = _initiator ? _c2 : _c1;
         Debug.Assert(cipher.HasKey());

         return cipher.DecryptWithAd(null, message, payloadBuffer);
      }

      public ulong GetNumberOfInitiatorMessages()
      {
         return _c1.GetNonce();
      }

      public ulong GetNumberOfResponderMessages()
      {
         return _c2.GetNonce();
      }

      public void KeyRecycleInitiatorToResponder()
      {
         Exceptions.ThrowIfDisposed(_disposed, nameof(Transport<TCipherType>));

         _c1.KeyRecycle();
      }

      public void KeyRecycleResponderToInitiator()
      {
         Exceptions.ThrowIfDisposed(_disposed, nameof(Transport<TCipherType>));

         if (IsOneWay)
         {
            throw new InvalidOperationException("Cannot rekey responder to initiator in a one-way stream.");
         }

         _c2.KeyRecycle();
      }

      public void Dispose()
      {
         if (!_disposed)
         {
            _c1.Dispose();
            _c2?.Dispose();
            _disposed = true;
         }
      }
   }
}