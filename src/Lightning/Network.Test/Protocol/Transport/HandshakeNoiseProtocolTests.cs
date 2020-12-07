using System.Buffers;
using MithrilShards.Core;
using Network.Protocol.Transport;
using Network.Protocol.Transport.Noise;
using Network.Test.Protocol.Transport.Noise;
using Xunit;

namespace Network.Test.Protocol.Transport
{
   public class HandshakeNoiseProtocolTests
   {
      private HandshakeNoiseProtocol _noiseProtocol;

      private static HandshakeNoiseProtocol GetInitiatorNoiseProtocol() =>
         new HandshakeNoiseProtocol(new PredefinedKeysNodeContext(
               new DefaultRandomNumberGenerator(), Bolt8TestVectorParameters.Initiator.PrivateKey)
            , Bolt8TestVectorParameters.Responder.PublicKey, new InitiatorTestHandshakeStateFactory());

      private static HandshakeNoiseProtocol GetResponderNoiseProtocol() =>
         new HandshakeNoiseProtocol(new PredefinedKeysNodeContext(
               new DefaultRandomNumberGenerator(), Bolt8TestVectorParameters.Responder.PrivateKey)
            , null, new ResponderTestHandshakeStateFactory());

      [Fact]
      public void FullHandshakeAndSendingMessageTest()
      {
         const string message = "0x68656c6c6f";

         var initiator = new HandshakeNoiseProtocol(new PredefinedKeysNodeContext(new DefaultRandomNumberGenerator(),
            Bolt8TestVectorParameters.Initiator.PrivateKey), Bolt8TestVectorParameters.Responder.PublicKey,
            new HandshakeStateFactory());

         var responder = new HandshakeNoiseProtocol(new PredefinedKeysNodeContext(new DefaultRandomNumberGenerator()
         , Bolt8TestVectorParameters.Responder.PrivateKey), null,
            new HandshakeStateFactory());

         //  act one initiator
         var input = new ReadOnlySequence<byte>();
         var output = new ArrayBufferWriter<byte>();
         initiator.Handshake(input, output);

         // act one & two responder
         input = new ReadOnlySequence<byte>(output.WrittenMemory.ToArray());
         output = new ArrayBufferWriter<byte>();
         responder.Handshake(input, output);

         // act two & three initiator
         input = new ReadOnlySequence<byte>(output.WrittenMemory.ToArray());
         output = new ArrayBufferWriter<byte>();
         initiator.Handshake(input, output);

         // act three responder
         input = new ReadOnlySequence<byte>(output.WrittenMemory.ToArray());
         output = new ArrayBufferWriter<byte>();
         responder.Handshake(input, output);

         // sending a message across initiator to responder
         input = new ReadOnlySequence<byte>(message.ToByteArray());
         output = new ArrayBufferWriter<byte>();
         initiator.WriteMessage(input, output);

         // responder receives the message
         input = new ReadOnlySequence<byte>(output.WrittenMemory.ToArray());
         output = new ArrayBufferWriter<byte>();
         int length = responder.ReadMessageLength(new ReadOnlySequence<byte>(input.Slice(0, responder.HeaderLength).ToArray()));
         responder.ReadMessage(input.Slice(initiator.HeaderLength, length), output);

         // check message decrypted are correctly
         Assert.Equal(output.WrittenSpan.ToArray(), message.ToByteArray());

         // sending a message across responder to initiator
         input = new ReadOnlySequence<byte>(message.ToByteArray());
         output = new ArrayBufferWriter<byte>();
         responder.WriteMessage(input, output);

         // initiator receives the message
         input = new ReadOnlySequence<byte>(output.WrittenMemory.ToArray());
         output = new ArrayBufferWriter<byte>();
         length = initiator.ReadMessageLength(new ReadOnlySequence<byte>(input.Slice(0, initiator.HeaderLength).ToArray()));
         initiator.ReadMessage(input.Slice(initiator.HeaderLength, length), output);

         // check message decrypted are correctly
         Assert.Equal(output.WrittenSpan.ToArray(), message.ToByteArray());
      }
      
      [Theory]
      [InlineData(Bolt8TestVectorParameters.ActOne.INITIATOR_OUTPUT)]
      public void TestHandshakeActOneInitiatorSide(string expectedOutputHex)
      {
         _noiseProtocol = GetInitiatorNoiseProtocol();

         var buffer = new ArrayBufferWriter<byte>();

         _noiseProtocol.Handshake(new ReadOnlySequence<byte>(), buffer);

         var expectedOutput = expectedOutputHex.ToByteArray();

         Assert.Equal(buffer.WrittenCount, expectedOutput.Length);
         Assert.Equal(buffer.WrittenSpan.ToArray(), expectedOutput);
      }

      [Theory]
      [InlineData(Bolt8TestVectorParameters.ActTwo.RESPONDER_OUTPUT)]
      public void TestHandshakeActTwoInitiatorSide(string expectedInputHex)
      {
         _noiseProtocol = GetInitiatorNoiseProtocol();

         var buffer = new ArrayBufferWriter<byte>();

         //write
         _noiseProtocol.Handshake(new ReadOnlySequence<byte>(), buffer);
         buffer.Clear();

         //read & write
         _noiseProtocol.Handshake(new ReadOnlySequence<byte>(expectedInputHex.ToByteArray()), buffer);

         //encrypted null so nothing to decrypt
         Assert.Equal(buffer.WrittenCount, 66);
         Assert.NotEmpty(buffer.WrittenSpan.ToArray());
      }

      [Theory]
      [InlineData(Bolt8TestVectorParameters.ActTwo.RESPONDER_OUTPUT,
         Bolt8TestVectorParameters.ActThree.INITIATOR_OUTPUT)]
      public void TestHandshakeActThreeInitiatorSide(string actTwoInput, string expectedOutputHex)
      {
         _noiseProtocol = GetInitiatorNoiseProtocol();

         var buffer = new ArrayBufferWriter<byte>();

         //write
         _noiseProtocol.Handshake(new ReadOnlySequence<byte>(), buffer);
         buffer.Clear();

         //read & write
         _noiseProtocol.Handshake(new ReadOnlySequence<byte>(actTwoInput.ToByteArray()), buffer);

         var expectedOutput = expectedOutputHex.ToByteArray();

         Assert.Equal(buffer.WrittenCount, expectedOutput.Length);
         Assert.Equal(buffer.WrittenSpan.ToArray(), expectedOutput);
      }

      [Theory]
      [InlineData(Bolt8TestVectorParameters.ActOne.INITIATOR_OUTPUT,
         Bolt8TestVectorParameters.ActTwo.RESPONDER_OUTPUT)]
      public void TestHandshakeActTwoResponderSide(string actOneInputHex, string expectedOutputHex)
      {
         _noiseProtocol = GetResponderNoiseProtocol();

         var buffer = new ArrayBufferWriter<byte>();

         // write and read
         _noiseProtocol.Handshake(new ReadOnlySequence<byte>(actOneInputHex.ToByteArray()), buffer);

         var expectedOutput = expectedOutputHex.ToByteArray();

         //encrypted null so nothing to decrypt
         Assert.Equal(buffer.WrittenCount, expectedOutput.Length);
         Assert.Equal(buffer.WrittenSpan.ToArray(), expectedOutput);
      }

      [Theory]
      [InlineData(Bolt8TestVectorParameters.ActOne.INITIATOR_OUTPUT,
         Bolt8TestVectorParameters.ActThree.INITIATOR_OUTPUT)]
      public void TestHandshakeActThreeResponderSide(string actOneInput, string actThreeInput)
      {
         _noiseProtocol = GetResponderNoiseProtocol();

         var buffer = new ArrayBufferWriter<byte>();

         //read
         _noiseProtocol.Handshake(new ReadOnlySequence<byte>(actOneInput.ToByteArray()), buffer);
         buffer.Clear();

         //write
         _noiseProtocol.Handshake(new ReadOnlySequence<byte>(actThreeInput.ToByteArray()), buffer);

         Assert.Equal(buffer.WrittenCount, 0);
         Assert.Empty(buffer.WrittenSpan.ToArray());
      }

      public class PredefinedKeysNodeContext : NodeContext
      {
         public PredefinedKeysNodeContext(IRandomNumberGenerator randomNumberGenerator,
            byte[] privateKey)
            : base(randomNumberGenerator)
         {
            PrivateKey = privateKey;
         }
      }

      public class InitiatorTestHandshakeStateFactory : IHandshakeStateFactory
      {
         public IHandshakeState CreateLightningNetworkHandshakeState(byte[] privateKey, byte[]? remotePublicKey)
         {
            var protocol = Network.Protocol.Transport.Noise.Protocol.Parse(LightningNetworkConfig.PROTOCOL_NAME);

            protocol.VersionPrefix = LightningNetworkConfig.NoiseProtocolVersionPrefix;

            var handshake = protocol.Create(remotePublicKey != null,
                  LightningNetworkConfig.ProlugeByteArray(),
                  privateKey,
                  remotePublicKey!)

               as HandshakeState<ChaCha20Poly1305, CurveSecp256K1, Sha256>;

            handshake?.SetDh(new DhWrapperWithDefinedEphemeralKey(new KeyPair(
               Bolt8TestVectorParameters.InitiatorEphemeralKeyPair.PrivateKey,
               Bolt8TestVectorParameters.InitiatorEphemeralKeyPair.PublicKey)));

            return handshake;
         }
      }

      public class ResponderTestHandshakeStateFactory : IHandshakeStateFactory
      {
         public IHandshakeState CreateLightningNetworkHandshakeState(byte[] privateKey, byte[]? remotePublicKey)
         {
            var protocol = Network.Protocol.Transport.Noise.Protocol.Parse(LightningNetworkConfig.PROTOCOL_NAME);

            protocol.VersionPrefix = LightningNetworkConfig.NoiseProtocolVersionPrefix;

            var handshake = protocol.Create(remotePublicKey != null,
                  LightningNetworkConfig.ProlugeByteArray(),
                  privateKey,
                  remotePublicKey!)

               as HandshakeState<ChaCha20Poly1305, CurveSecp256K1, Sha256>;

            handshake?.SetDh(new DhWrapperWithDefinedEphemeralKey(new KeyPair(
               Bolt8TestVectorParameters.ResponderEphemeralKeyPair.PrivateKey,
               Bolt8TestVectorParameters.ResponderEphemeralKeyPair.PublicKey)));

            return handshake;
         }
      }
   }
}