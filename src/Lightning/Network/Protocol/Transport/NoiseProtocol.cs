using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.Serialization;
using Network.Protocol.Transport.Noise;

namespace Network.Protocol.Transport
{
   public class HandshakeNoiseProtocol : IHandshakeProtocol
   {
      private const int HEADER_LENGTH = 18;

      public byte[]? RemotePubKey { get; set; }
      public string LocalPubKey { get; set; }

      public bool Initiator { get; set; }

      public byte[] PrivateKey { get; set; } // TODO: this can be private or even hidden behind an interface.

      private readonly IHandshakeState _handshakeState;

      private ITransport? _transport;

      private readonly byte[] _messageHeaderCache = new byte[2];

      public HandshakeNoiseProtocol(NodeContext nodeContext, byte[]? remotePubKey,
         IHandshakeStateFactory handshakeFactory)
      {
         PrivateKey = nodeContext.PrivateKey;
         LocalPubKey = nodeContext.LocalPubKey;
         if (remotePubKey != null)
         {
            RemotePubKey = remotePubKey;
            Initiator = true;
         }

         _handshakeState = handshakeFactory.CreateLightningNetworkHandshakeState(PrivateKey, RemotePubKey!);
      }

      public int HeaderLength { get { return HEADER_LENGTH; } }

      public void WriteMessage(ReadOnlySequence<byte> message, IBufferWriter<byte> output) //TODO David add tests
      {
         if (_transport == null)
            throw new InvalidOperationException("Must complete handshake before reading messages");

         BinaryPrimitives.WriteUInt16BigEndian(_messageHeaderCache, Convert.ToUInt16(message.Length));

         int headerLength = _transport.WriteMessage(_messageHeaderCache, output.GetSpan());

         output.Advance(headerLength);

         int messageLength = _transport.WriteMessage(message.FirstSpan, output.GetSpan());

         output.Advance(messageLength);

         HandleKeyRecycle();
      }

      public void ReadMessage(ReadOnlySequence<byte> message, IBufferWriter<byte> output) //TODO David add tests
      {
         if (_transport == null)
            throw new InvalidOperationException("Must complete handshake before reading messages");

         int bytesRead = _transport.ReadMessage(message.FirstSpan, output.GetSpan()); // TODO check what if buffer is very big

         output.Advance(bytesRead);

         HandleKeyRecycle();
      }

      private void HandleKeyRecycle() //TODO David add tests
      {
         if (_transport == null)
            return;

         if (_transport.GetNumberOfInitiatorMessages() == LightningNetworkConfig.NumberOfNonceBeforeKeyRecycle)
            _transport.KeyRecycleInitiatorToResponder();

         if (_transport.GetNumberOfResponderMessages() == LightningNetworkConfig.NumberOfNonceBeforeKeyRecycle)
            _transport.KeyRecycleResponderToInitiator();
      }

      public int ReadMessageLength(ReadOnlySequence<byte> encryptedHeader) //TODO David add tests
      {
         if (_transport == null)
            throw new InvalidOperationException("Must complete handshake before reading messages");

         _transport.ReadMessage(encryptedHeader.FirstSpan, _messageHeaderCache);

         ushort messageLengthDecrypted = (ushort)BinaryPrimitives.ReadUInt16BigEndian(_messageHeaderCache); // TODO Dan test header size bigger then 2 bytes

         // Return the message length plus the 16 byte mac data
         // the caller does not need to know the message has mac data
         return messageLengthDecrypted + Aead.TAG_SIZE;
      }

      public void Handshake(ReadOnlySequence<byte> message, IBufferWriter<byte> output)
      {
         if (Initiator)
         {
            if (message.Length == 0)
            {
               (int bytesWritten, _, _) = _handshakeState.WriteMessage(null, output.GetSpan());

               output.Advance(bytesWritten);
            }
            else
            {
               _handshakeState.ReadMessage(message.FirstSpan, output.GetSpan());

               (int bytesWritten, _, ITransport? transport) = _handshakeState.WriteMessage(null, output.GetSpan());

               output.Advance(bytesWritten);

               _transport = transport ?? throw new InvalidOperationException(nameof(transport));

               _handshakeState.Dispose();
            }
         }
         else
         {
            (_, _, ITransport? transport) = _handshakeState.ReadMessage(message.FirstSpan, output.GetSpan());

            if (transport == null)
            {
               (int bytesWritten, _, _) = _handshakeState.WriteMessage(null, output.GetSpan());

               output.Advance(bytesWritten);
            }
            else
               _transport = transport;
         }
      }
   }
}