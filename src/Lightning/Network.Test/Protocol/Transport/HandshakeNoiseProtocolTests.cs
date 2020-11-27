using System;
using System.Buffers;
using MithrilShards.Core;
using Network.Protocol.Transport;
using Network.Test.Protocol.Transport.Noise;
using Xunit;

namespace Network.Test.Protocol.Transport
{
   public class HandshakeNoiseProtocolTests
   {
      private class PredefinedKeysNodeContext : NodeContext
      {
         public PredefinedKeysNodeContext(IRandomNumberGenerator randomNumberGenerator,
            byte[] privateKey) 
            : base(randomNumberGenerator)
         {
            PrivateKey = privateKey;
         }
      }

      [Fact]
      public void HandshakeCompleteAndMessagesSent()
      //TODO add tests after injecting a factory instead of new Protocol
      {
         string message = "0x68656c6c6f";
         
         var initiator = new HandshakeNoiseProtocol(new PredefinedKeysNodeContext(new DefaultRandomNumberGenerator(),
            Bolt8TestVectorParameters.Initiator.PrivateKey),Bolt8TestVectorParameters.Receiver.PublicKey);

         var responder = new HandshakeNoiseProtocol(new PredefinedKeysNodeContext(new DefaultRandomNumberGenerator()
         ,Bolt8TestVectorParameters.Receiver.PrivateKey), null);

         var buffer = new ArrayBufferWriter<byte>(66);

         var input = new byte[66];
         
         initiator.Handshake(null,buffer);
         buffer.WrittenSpan.CopyTo(input.AsSpan(0, 50));
         buffer.Clear();
         responder.Handshake(input.AsSpan(0,50),buffer);

         buffer.WrittenSpan.CopyTo(input.AsSpan(0, 50));
         buffer.Clear();
         initiator.Handshake(input.AsSpan(0,50),buffer);
         
         buffer.Clear();
         
         initiator.Handshake(null,buffer);
         buffer.WrittenSpan.CopyTo(input.AsSpan());
         buffer.Clear();
         responder.Handshake(input.AsSpan(),buffer);
         
         initiator.WriteMessage(message.ToByteArray(),buffer);
         buffer.WrittenSpan.CopyTo(input.AsSpan());
         buffer.Clear();
         responder.ReadMessage(input.AsSpan(0,39),buffer);
      }
   }
}