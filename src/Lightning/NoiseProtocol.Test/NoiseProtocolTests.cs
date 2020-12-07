using System.Buffers;
using MithrilShards.Core;
using Network.Protocol.Transport;
using Network.Test.Protocol.Transport;
using Network.Test.Protocol.Transport.Noise;
using Xunit;

namespace NoiseProtocol.Test
{
   public class NoiseProtocolTests
   {
      const string MESSAGE = "0x68656c6c6f";
      
      [Fact]
      public void TestNewNoiseResponderCommWithExistingInitiator()
      {
         var initiator = new HandshakeNoiseProtocol(new HandshakeNoiseProtocolTests.PredefinedKeysNodeContext(new DefaultRandomNumberGenerator(),
            Bolt8TestVectorParameters.Initiator.PrivateKey), Bolt8TestVectorParameters.Responder.PublicKey,
            new HandshakeNoiseProtocolTests.InitiatorTestHandshakeStateFactory());

         var responder = new NoiseProtocol(Bolt8TestVectorParameters.Responder.PrivateKey);
         responder.InitHandShake();

         //  act one initiator
         var input = new ReadOnlySequence<byte>();
         var output = new ArrayBufferWriter<byte>();
         initiator.Handshake(input, output);

         // act one & two responder
         input = new ReadOnlySequence<byte>(output.WrittenMemory.ToArray());
         output = new ArrayBufferWriter<byte>();
         var actTwo = responder.ProcessHandshakeRequest(input.FirstSpan);

         // act two & three initiator
         input = new ReadOnlySequence<byte>(actTwo.ToArray());
         output = new ArrayBufferWriter<byte>();
         initiator.Handshake(input, output);

         // act three responder
         input = new ReadOnlySequence<byte>(output.WrittenMemory.ToArray());
         responder.CompleteHandshake(input.FirstSpan);
      }
      
      [Fact]
      public void TestNewNoiseInitiatorWithExistingResponder()
      {
         var initiator =  new NoiseProtocol(Bolt8TestVectorParameters.Initiator.PrivateKey);
         initiator.InitHandShake();

         var responder = new HandshakeNoiseProtocol(new HandshakeNoiseProtocolTests.PredefinedKeysNodeContext(
               new DefaultRandomNumberGenerator(),
               Bolt8TestVectorParameters.Responder.PrivateKey),
            null,
            new HandshakeNoiseProtocolTests.ResponderTestHandshakeStateFactory());

         //  act one initiator
         var input = new ReadOnlySequence<byte>();
         var output = new ArrayBufferWriter<byte>();
         var actOne = initiator.StartNewHandshake(Bolt8TestVectorParameters.Responder.PublicKey);

         // act one & two responder
         input = new ReadOnlySequence<byte>(actOne.ToArray());
         output = new ArrayBufferWriter<byte>();
         responder.Handshake(input, output);

         // act two & three initiator
         input = new ReadOnlySequence<byte>(output.WrittenMemory.ToArray());
         output = new ArrayBufferWriter<byte>();
         var actThree = initiator.ProcessHandshakeRequest(input.ToArray());

         // act three responder
         input = new ReadOnlySequence<byte>(actThree.ToArray());
         output = new ArrayBufferWriter<byte>();
         responder.Handshake(input, output);
      }
   }
}