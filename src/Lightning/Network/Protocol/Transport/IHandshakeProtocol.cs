using System;
using System.Buffers;

namespace Network.Protocol.Transport
{
   public interface IHandshakeProtocol
   {
      public byte[] RemotePubKey { get; set; }
      public string? LocalPubKey { get; set; }

      public bool Initiator { get; set; }

      public byte[]? PrivateKey { get; set; }

      public int HeaderLength { get; }

      public void WriteMessage(ReadOnlySequence<byte> message, IBufferWriter<byte> output);

      public void ReadMessage(ReadOnlySequence<byte> message, IBufferWriter<byte> output);

      public int ReadMessageLength(ReadOnlySpan<byte> encryptedHeader);

      public void Handshake(ReadOnlySequence<byte> message, IBufferWriter<byte> output);
   }
}