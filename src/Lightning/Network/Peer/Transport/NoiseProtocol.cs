using System;
using System.Buffers;
using MithrilShards.Core.DataTypes;

namespace Network.Peer.Transport
{
   public interface IHandshakePotocol
   {
      public UInt256 RemotePubKey { get; set; }
      public UInt256 LocalPubKey { get; set; }

      public bool Initiator { get; set; }

      public byte[] PrivateLey { get; set; }

      public void WriteMessage(ReadOnlySpan<byte> message, IBufferWriter<byte> output);

      public void ReadMessage(ReadOnlySpan<byte> message, IBufferWriter<byte> output);

      public void Handshake(ReadOnlySpan<byte> message, IBufferWriter<byte> output);
   }

   public class HandshakeNoisePotocol : IHandshakePotocol
   {
      public UInt256 RemotePubKey { get; set; }
      public UInt256 LocalPubKey { get; set; }

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
         output.Write(message);
      }
   }
}