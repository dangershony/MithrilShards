using System.Buffers;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.Utils;
using Moq;
using Xunit;

namespace NoiseProtocol.Test
{
   public class HandshakeProcessorTests
   {
      const string MESSAGE = "0x68656c6c6f";
      
      static HandshakeProcessor NewNoiseProtocol() =>
         new HandshakeProcessor(new EllipticCurveActions(), new Hkdf(new HashWithState(), new HashWithState()), 
            new ChaCha20Poly1305CipherFunction(new Mock<ILogger<ChaCha20Poly1305CipherFunction>>().Object)
            , new KeyGenerator(), new Sha256(new Mock<ILogger<Sha256>>().Object),
            new NoiseMessageTransformer(new Hkdf(new HashWithState(), new HashWithState()),
               new ChaCha20Poly1305CipherFunction(new Mock<ILogger<ChaCha20Poly1305CipherFunction>>().Object),
               new ChaCha20Poly1305CipherFunction(new Mock<ILogger<ChaCha20Poly1305CipherFunction>>().Object),
               new Mock<ILogger<NoiseMessageTransformer>>().Object),new Mock<ILogger<HandshakeProcessor>>().Object);
      
      [Fact]
      public void FullHandshakeAndSendingMessageTest()
      {
         var initiator = NewNoiseProtocol();
         initiator.InitiateHandShake(Bolt8TestVectorParameters.Initiator.PrivateKey);
         var responder = NewNoiseProtocol();
         responder.InitiateHandShake(Bolt8TestVectorParameters.Responder.PrivateKey);
   
         //  act one initiator
         var input = new ReadOnlySequence<byte>();
         var output = new ArrayBufferWriter<byte>();
         initiator.StartNewInitiatorHandshake(Bolt8TestVectorParameters.Responder.PublicKey, output);
   
         // act one & two responder
         input = new ReadOnlySequence<byte>(output.WrittenMemory);
         output = new ArrayBufferWriter<byte>();
         responder.ProcessHandshakeRequest(input, output);
   
         // act two & three initiator
         input = new ReadOnlySequence<byte>(output.WrittenMemory);
         output = new ArrayBufferWriter<byte>();
         initiator.ProcessHandshakeRequest(input, output);
   
         // act three responder
         input = new ReadOnlySequence<byte>(output.WrittenMemory);
         responder.CompleteResponderHandshake(input);
         
         // complete handshake
         var initiatorTransformer = initiator.GetMessageTransformer();
         var responderTransformer = responder.GetMessageTransformer();

         byte[] message = MESSAGE.ToByteArray();
         
         // sending a message across initiator to responder
         input = new ReadOnlySequence<byte>(message);
         output = new ArrayBufferWriter<byte>();
         initiatorTransformer.WriteMessage(input, output);

         // responder receives the message
         input = new ReadOnlySequence<byte>(output.WrittenMemory.ToArray());
         output = new ArrayBufferWriter<byte>();
         responderTransformer.ReadMessage(input, output);
         
         // check message decrypted are correctly
         Assert.Equal(output.WrittenSpan.ToArray(), message);

         // sending a message across responder to initiator
         input = new ReadOnlySequence<byte>(message);
         output = new ArrayBufferWriter<byte>();
         responderTransformer.WriteMessage(input, output);

         // initiator receives the message
         input = new ReadOnlySequence<byte>(output.WrittenMemory.ToArray());
         output = new ArrayBufferWriter<byte>();
         initiatorTransformer.ReadMessage(input, output);

         // check message decrypted are correctly
         Assert.Equal(output.WrittenSpan.ToArray(), message);
      }
   }
}