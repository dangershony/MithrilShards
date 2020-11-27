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

      public void WriteMessage(ReadOnlySpan<byte> message, IBufferWriter<byte> output);

      public void ReadMessage(ReadOnlySpan<byte> message, IBufferWriter<byte> output);

      public int ReadMessageLength(ReadOnlySequence<byte> encryptedHeader);

      public void Handshake(ReadOnlySpan<byte> message, IBufferWriter<byte> output);
   }
}