using System;
using System.Buffers;
using Network.Protocol.Transport.Noise;

namespace Network.Protocol.Transport
{
   public class HandshakeNoiseProtocol : IHandshakeProtocol
   {
      public byte[] RemotePubKey { get; set; }
      public string LocalPubKey { get; set; }

      public bool Initiator { get; set; }

      public byte[] PrivateLey { get; set; } // TODO: this can be private or even hidden behind an interface.

      private readonly IHandshakeState _handshakeState;
      
      private ITransport _transport;

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
            PrivateLey, RemotePubKey);
      }
      
      public void WriteMessage(ReadOnlySpan<byte> message, IBufferWriter<byte> output)
      {
         output.Write(message);
      }

      public void ReadMessage(ReadOnlySpan<byte> message, IBufferWriter<byte> output)
      {
         output.Write(message);
      }

      public long ReadMessageLength(ReadOnlySequence<byte> encryptedHeader)
      {
         return 5;
      }

      public void Handshake(ReadOnlySpan<byte> message, IBufferWriter<byte> output)
      {
         output.Write(new byte[5] { 1, 2, 3, 4, 5 });
      }
   }
}