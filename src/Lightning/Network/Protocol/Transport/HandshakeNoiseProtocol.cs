using System;
using System.Buffers;
using System.Buffers.Binary;
using Network.Protocol.Transport.Noise;
using NoiseProtocol;
using LightningNetworkConfig = Network.Protocol.Transport.Noise.LightningNetworkConfig;

namespace Network.Protocol.Transport
{
   public class HandshakeWithNoiseProtocol : IHandshakeProtocol
   {
      private const int HEADER_LENGTH = 18;

      public byte[]? RemotePubKey { get; set; }
      public string LocalPubKey { get; set; }

      public bool Initiator { get; set; }

      public byte[] PrivateKey { get; set; } // TODO: this can be private or even hidden behind an interface.

      // private readonly IHandshakeState _handshakeState;
      //
      // private ITransport? _transport;

      private readonly ArrayBufferWriter<byte> _messageHeaderCache = new ArrayBufferWriter<byte>(2);
      readonly INoiseProtocol _noiseProtocol;
      INoiseMessageTransformer _transport;

      public HandshakeWithNoiseProtocol(NodeContext nodeContext, byte[]? remotePubKey,
         INoiseProtocol noiseProtocol)
      {
         PrivateKey = nodeContext.PrivateKey;
         LocalPubKey = nodeContext.LocalPubKey;
         if (remotePubKey != null)
         {
            RemotePubKey = remotePubKey;
            Initiator = true;
         }

         _noiseProtocol = noiseProtocol;
         _noiseProtocol.SetPrivateKey(PrivateKey);
         _noiseProtocol.InitHandShake();
         
        // _handshakeState = handshakeFactory.CreateLightningNetworkHandshakeState(PrivateKey, RemotePubKey!);
      }

      public int HeaderLength { get { return HEADER_LENGTH; } }

      public void WriteMessage(ReadOnlySequence<byte> message, IBufferWriter<byte> output) //TODO David add tests
      {
         if (_transport == null)
            throw new InvalidOperationException("Must complete handshake before reading messages");

         _messageHeaderCache.Clear();
         
         BinaryPrimitives.WriteUInt16BigEndian(_messageHeaderCache.GetSpan(), Convert.ToUInt16(message.Length));

         _messageHeaderCache.Advance(2);
         
         _transport.WriteMessage(new ReadOnlySequence<byte>(_messageHeaderCache.WrittenMemory),
            output);

         _transport.WriteMessage(message, output);
      }

      public void ReadMessage(ReadOnlySequence<byte> message, IBufferWriter<byte> output) //TODO David add tests
      {
         if (_transport == null)
            throw new InvalidOperationException("Must complete handshake before reading messages");

         _transport.ReadMessage(message, output); // TODO check what if buffer is very big
      }

      public int ReadMessageLength(ReadOnlySequence<byte> encryptedHeader) //TODO David add tests
      {
         if (_transport == null)
            throw new InvalidOperationException("Must complete handshake before reading messages");

         _messageHeaderCache.Clear();
         
         _transport.ReadMessage(encryptedHeader, _messageHeaderCache);

         ushort messageLengthDecrypted = BinaryPrimitives.ReadUInt16BigEndian(_messageHeaderCache.WrittenSpan); // TODO Dan test header size bigger then 2 bytes

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
               _noiseProtocol.StartNewInitiatorHandshake(RemotePubKey,output);
            }
            else
            {
               _noiseProtocol.ProcessHandshakeRequest(message,output);

               _transport = _noiseProtocol.GetMessageTransformer();
            }
         }
         else
         {
            if (message.Length == 66)
            {
               _noiseProtocol.CompleteResponderHandshake(message);
               _transport = _noiseProtocol.GetMessageTransformer();
            }
            else
            {
               _noiseProtocol.ProcessHandshakeRequest(message,output);
            }
         }
      }
   }
}