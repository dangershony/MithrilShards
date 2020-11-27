using System;
using System.Buffers;
using System.Buffers.Binary;
using Network.Protocol.Transport.Noise;

namespace Network.Protocol.Transport
{
   public class HandshakeNoiseProtocol : IHandshakeProtocol
   {
      public byte[]? RemotePubKey { get; set; }
      public string LocalPubKey { get; set; }

      public bool Initiator { get; set; }

      public byte[] PrivateLey { get; set; } // TODO: this can be private or even hidden behind an interface.

      private readonly IHandshakeState _handshakeState;

      private ITransport? _transport;

      private byte[] _messageHeader = new byte[2];

      public HandshakeNoiseProtocol(NodeContext nodeContext, byte[]? remotePubKey)
      {
         PrivateLey = nodeContext.PrivateKey;
         LocalPubKey = nodeContext.LocalPubKey;
         if (remotePubKey != null)
         {
            RemotePubKey = remotePubKey;
            Initiator = true;
         }

         Noise.Protocol protocol = Noise.Protocol.Parse(LightningNetworkConfig.PROTOCOL_NAME);

         protocol.VersionPrefix = LightningNetworkConfig.NoiseProtocolVersionPrefix;

         _handshakeState = protocol.Create(Initiator, LightningNetworkConfig.ProlugeByteArray(),
            PrivateLey, RemotePubKey!);
      }

      public void WriteMessage(ReadOnlySpan<byte> message, IBufferWriter<byte> output) //TODO David add tests
      {
         if (_transport == null)
            throw new InvalidOperationException("Must complete handshake before reading messages");

         BinaryPrimitives.WriteUInt16BigEndian(_messageHeader,Convert.ToUInt16(message.Length));

         int headerLength = _transport.WriteMessage(_messageHeader, output.GetSpan() );

         output.Advance(headerLength);

         int messageLength = _transport.WriteMessage(message, output.GetSpan());

         output.Advance(messageLength);

         HandleKeyRecycle();
      }

      public void ReadMessage(ReadOnlySpan<byte> message, IBufferWriter<byte> output) //TODO David add tests
      {
         if (_transport == null)
            throw new InvalidOperationException("Must complete handshake before reading messages");

         _transport.ReadMessage(message.Slice(0, 18), _messageHeader);

         short bodyLength = BinaryPrimitives.ReadInt16BigEndian(_messageHeader);

         _transport.ReadMessage(message.Slice(18), output.GetSpan(bodyLength));

         output.Advance(bodyLength);

         HandleKeyRecycle();
      }

      void HandleKeyRecycle() //TODO David add tests
      {
         if (_transport == null)
            return;

         if (_transport.GetNumberOfInitiatorMessages() == LightningNetworkConfig.NumberOfNonceBeforeKeyRecycle)
            _transport.KeyRecycleInitiatorToResponder();

         if (_transport.GetNumberOfResponderMessages() == LightningNetworkConfig.NumberOfNonceBeforeKeyRecycle)
            _transport.KeyRecycleResponderToInitiator();
      }

      public ushort ReadMessageLength(ReadOnlySequence<byte> encryptedHeader) //TODO David add tests
      {
         if (_transport == null)
            throw new InvalidOperationException("Must complete handshake before reading messages");

         _transport.ReadMessage(encryptedHeader.FirstSpan, _messageHeader);

         return BinaryPrimitives.ReadUInt16BigEndian(_messageHeader);
      }

      public void Handshake(ReadOnlySpan<byte> message, IBufferWriter<byte> output)
      {
         if (Initiator)
         {
            if (message == null)
            {
               (int bytesWritten, _, ITransport? transport) = _handshakeState.WriteMessage(null, output.GetSpan());

               output.Advance(bytesWritten);

               if (transport == null)
                  return;

               _transport = transport;
               _handshakeState.Dispose();
            }
            else
            {
               _handshakeState.ReadMessage(message, output.GetSpan());
            }
         }
         else
         {
            (_, _, ITransport? transport) = _handshakeState.ReadMessage(message, output.GetSpan());

            if (transport == null)
            {
               (int bytesWritten, _, _) =  _handshakeState.WriteMessage(null, output.GetSpan());

               output.Advance(bytesWritten);
            }
            else
               _transport = transport;
         }
      }
   }
}