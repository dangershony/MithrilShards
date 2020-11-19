using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using MithrilShards.Core.DataTypes;

namespace Network.Transport
{
   public interface INoiseProtocol
   {
      public UInt256 RemotePubKey { get; set; }
      public UInt256 LocalPubKey { get; set; }

      public bool Initiator { get; set; }

      public byte[] PrivateLey { get; set; }

      public void Encrypt(ReadOnlySpan<byte> message, IBufferWriter<byte> output);

      public void Decrypt(ReadOnlySpan<byte> message, IBufferWriter<byte> output);

      public void Handshake(ReadOnlySpan<byte> message, IBufferWriter<byte> output);
   }

   public class NoiseProtocol : INoiseProtocol
   {
      public UInt256 RemotePubKey { get; set; }
      public UInt256 LocalPubKey { get; set; }

      public bool Initiator { get; set; }

      public byte[] PrivateLey { get; set; } // TODO: this can be private or even hidden behind an interface.

      public void Encrypt(ReadOnlySpan<byte> message, IBufferWriter<byte> output)
      {
         output.Write(message);
      }

      public void Decrypt(ReadOnlySpan<byte> message, IBufferWriter<byte> output)
      {
         output.Write(message);
      }

      public void Handshake(ReadOnlySpan<byte> message, IBufferWriter<byte> output)
      {
         output.Write(message);
      }
   }
}