using System;
using System.Buffers;
using Bitcoin.Primitives.Fundamental;

namespace Network.Protocol.Transport
{
   public interface IHandshakeProtocol
   {
      public int HeaderLength { get; }

      public void WriteMessage(ReadOnlySequence<byte> message, IBufferWriter<byte> output);

      public void ReadMessage(ReadOnlySequence<byte> message, IBufferWriter<byte> output);

      public int ReadMessageLength(ReadOnlySequence<byte> encryptedHeader);

      public void Handshake(ReadOnlySequence<byte> message, IBufferWriter<byte> output);
   }
}