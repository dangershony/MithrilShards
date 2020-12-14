using System.Buffers;
using MithrilShards.Core;
using Network.Protocol.Transport;
using Network.Test.Protocol.Transport;
using Network.Test.Protocol.Transport.Noise;
using Xunit;

namespace NoiseProtocol.Test
{
   // public class NoiseProtocolWithExistingNoiseTests
   // {
   //    const string MESSAGE = "0x68656c6c6f";
   //    
   //    [Fact]
   //    public void TestNewNoiseResponderCommWithExistingInitiator()
   //    {
   //       var initiator = new HandshakeWithNoiseProtocol(new HandshakeNoiseProtocolTests.PredefinedKeysNodeContext(new DefaultRandomNumberGenerator(),
   //          Bolt8TestVectorParameters.Initiator.PrivateKey), Bolt8TestVectorParameters.Responder.PublicKey,
   //          new HandshakeNoiseProtocolTests.InitiatorTestHandshakeStateFactory());
   //
   //       var hkdf = new OldHkdf(new OldHash(), new OldHash());
   //       
   //       var responder = new NoiseProtocol(new EllipticCurveActions(), hkdf, new ChaCha20Poly1305CipherFunction()
   //          , new KeyGenerator(), new HashFunction(),new NoiseMessageTransformer(hkdf,
   //             new ChaCha20Poly1305CipherFunction(), new ChaCha20Poly1305CipherFunction()),  
   //          Bolt8TestVectorParameters.Responder.PrivateKey);
   //       responder.InitHandShake();
   //
   //       //  act one initiator
   //       var input = new ReadOnlySequence<byte>();
   //       var output = new ArrayBufferWriter<byte>();
   //       initiator.Handshake(input, output);
   //
   //       // act one & two responder
   //       input = new ReadOnlySequence<byte>(output.WrittenMemory);
   //       output = new ArrayBufferWriter<byte>();
   //       responder.ProcessHandshakeRequest(input.FirstSpan, output);
   //
   //       // act two & three initiator
   //       input = new ReadOnlySequence<byte>(output.WrittenMemory);
   //       output = new ArrayBufferWriter<byte>();
   //       initiator.Handshake(input, output);
   //
   //       // act three responder
   //       input = new ReadOnlySequence<byte>(output.WrittenMemory);
   //       responder.CompleteResponderHandshake(input.FirstSpan);
   //    }
   //    
   //    [Fact]
   //    public void TestNewNoiseInitiatorWithExistingResponder()
   //    {
   //       var hkdf = new OldHkdf(new OldHash(), new OldHash());
   //       
   //       var initiator =  new NoiseProtocol(new EllipticCurveActions(),hkdf, new ChaCha20Poly1305CipherFunction(),
   //          new KeyGenerator(), new HashFunction(),new NoiseMessageTransformer(hkdf,
   //             new ChaCha20Poly1305CipherFunction(), new ChaCha20Poly1305CipherFunction()),
   //          Bolt8TestVectorParameters.Initiator.PrivateKey);
   //       initiator.InitHandShake();
   //
   //       var responder = new HandshakeWithNoiseProtocol(new HandshakeNoiseProtocolTests.PredefinedKeysNodeContext(
   //             new DefaultRandomNumberGenerator(),
   //             Bolt8TestVectorParameters.Responder.PrivateKey),
   //          null,
   //          new HandshakeNoiseProtocolTests.ResponderTestHandshakeStateFactory());
   //
   //       //  act one initiator
   //       var output = new ArrayBufferWriter<byte>();
   //       initiator.StartNewInitiatorHandshake(Bolt8TestVectorParameters.Responder.PublicKey, output);
   //
   //       // act one & two responder
   //       var input = new ReadOnlySequence<byte>(output.WrittenMemory);
   //       output = new ArrayBufferWriter<byte>();
   //       responder.Handshake(input, output);
   //
   //       // act two & three initiator
   //       input = new ReadOnlySequence<byte>(output.WrittenMemory);
   //       output = new ArrayBufferWriter<byte>();
   //       initiator.ProcessHandshakeRequest(input.FirstSpan, output);
   //
   //       // act three responder
   //       input = new ReadOnlySequence<byte>(output.WrittenMemory);
   //       output = new ArrayBufferWriter<byte>();
   //       responder.Handshake(input, output);
   //    }
   // }
}