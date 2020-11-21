using System;
using System.Buffers;

namespace Network.Protocol.Transport
{
   public interface IHandshakeProtocol
   {
      public string RemotePubKey { get; set; }
      public string LocalPubKey { get; set; }

      public bool Initiator { get; set; }

      public byte[] PrivateLey { get; set; }

      public void WriteMessage(ReadOnlySpan<byte> message, IBufferWriter<byte> output);

      public void ReadMessage(ReadOnlySpan<byte> message, IBufferWriter<byte> output);

      public void Handshake(ReadOnlySpan<byte> message, IBufferWriter<byte> output);
   }

   public class HandshakeNoiseProtocol : IHandshakeProtocol
   {
      public string RemotePubKey { get; set; }
      public string LocalPubKey { get; set; }

      public bool Initiator { get; set; }

      public byte[] PrivateLey { get; set; } // TODO: this can be private or even hidden behind an interface.

      public void WriteMessage(ReadOnlySpan<byte> message, IBufferWriter<byte> output)
      {
         output.Write(message);
      }

      public void ReadMessage(ReadOnlySpan<byte> message, IBufferWriter<byte> output)
      {
         output.Write(message);
      }

      public void Handshake(ReadOnlySpan<byte> message, IBufferWriter<byte> output)
      {
         output.Write(new byte[5] { 1, 2, 3, 4, 5 });
      }
   }
}