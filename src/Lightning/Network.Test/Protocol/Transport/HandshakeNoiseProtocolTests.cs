using System;
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
               new DefaultRandomNumberGenerator(),Bolt8TestVectorParameters.Initiator.PrivateKey)
            ,Bolt8TestVectorParameters.Responder.PublicKey,new InitiatorTestHandshakeStateFactory());
      
      private static HandshakeNoiseProtocol GetResponderNoiseProtocol() =>
         new HandshakeNoiseProtocol(new PredefinedKeysNodeContext(
               new DefaultRandomNumberGenerator(),Bolt8TestVectorParameters.Responder.PrivateKey)
            ,null,new ResponderTestHandshakeStateFactory());
      
      [Fact]
      public void FullHandshakeAndSendingMessageTest()
      {
         const string message = "0x68656c6c6f";
         
         var initiator = new HandshakeNoiseProtocol(new PredefinedKeysNodeContext(new DefaultRandomNumberGenerator(),
            Bolt8TestVectorParameters.Initiator.PrivateKey),Bolt8TestVectorParameters.Responder.PublicKey,
            new HandshakeStateFactory());

         var responder = new HandshakeNoiseProtocol(new PredefinedKeysNodeContext(new DefaultRandomNumberGenerator()
         ,Bolt8TestVectorParameters.Responder.PrivateKey), null,
            new HandshakeStateFactory());

         var buffer = new ArrayBufferWriter<byte>(66);

         var input = new byte[66];
         
         //  act one 
         initiator.Handshake(null,buffer);
         buffer.WrittenSpan.CopyTo(input.AsSpan(0, 50));
         buffer.Clear();
         responder.Handshake(input.AsSpan(0,50),buffer);

         // act two
         buffer.WrittenSpan.CopyTo(input.AsSpan(0, 50));
         buffer.Clear();
         initiator.Handshake(input.AsSpan(0,50),buffer);
         
         buffer.Clear();
         
         // act three
         initiator.Handshake(null,buffer);
         buffer.WrittenSpan.CopyTo(input.AsSpan());
         buffer.Clear();
         responder.Handshake(input.AsSpan(),buffer);
         
         initiator.WriteMessage(message.ToByteArray(),buffer);
         buffer.WrittenSpan.CopyTo(input.AsSpan());
         buffer.Clear();
         responder.ReadMessage(input.AsSpan(0,39),buffer);
         
         Assert.Equal(buffer.WrittenSpan.ToArray(),message.ToByteArray());
      }

      [Theory]
      [InlineData(Bolt8TestVectorParameters.ActOne.INITIATOR_OUTPUT)]
      public void TestHandshakeActOneInitiatorSide(string expectedOutputHex)
      {
         _noiseProtocol = GetInitiatorNoiseProtocol();
      
         var buffer = new ArrayBufferWriter<byte>();
      
         _noiseProtocol.Handshake(null,buffer);
         
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
         _noiseProtocol.Handshake(null,buffer);
         buffer.Clear();
         
         //read
         _noiseProtocol.Handshake(expectedInputHex.ToByteArray(),buffer);
         
         //encrypted null so nothing to decrypt
         Assert.Equal(buffer.WrittenCount, 0); 
         Assert.Empty(buffer.WrittenSpan.ToArray());
      }
      
      [Theory]
      [InlineData(Bolt8TestVectorParameters.ActTwo.RESPONDER_OUTPUT,
         Bolt8TestVectorParameters.ActThree.INITIATOR_OUTPUT)]
      public void TestHandshakeActThreeInitiatorSide(string actTwoInput,string expectedOutputHex)
      {
         _noiseProtocol = GetInitiatorNoiseProtocol();
      
         var buffer = new ArrayBufferWriter<byte>();
      
         //write
         _noiseProtocol.Handshake(null,buffer);
         buffer.Clear();
         
         //read
         _noiseProtocol.Handshake(actTwoInput.ToByteArray(),buffer);
         buffer.Clear();
         
         //write
         _noiseProtocol.Handshake(null,buffer);
         
         var expectedOutput = expectedOutputHex.ToByteArray();
      
         Assert.Equal(buffer.WrittenCount, expectedOutput.Length);
         Assert.Equal(buffer.WrittenSpan.ToArray(), expectedOutput);
      }

      [Theory]
      [InlineData(Bolt8TestVectorParameters.ActOne.INITIATOR_OUTPUT,
         Bolt8TestVectorParameters.ActTwo.RESPONDER_OUTPUT)]
      public void TestHandshakeActTwoResponderSide(string actOneInputHex,string expectedOutputHex)
      {
         _noiseProtocol = GetResponderNoiseProtocol();
      
         var buffer = new ArrayBufferWriter<byte>();
      
         // write and read 
         _noiseProtocol.Handshake(actOneInputHex.ToByteArray(),buffer);
         
         var expectedOutput = expectedOutputHex.ToByteArray();
         
         //encrypted null so nothing to decrypt
         Assert.Equal(buffer.WrittenCount, expectedOutput.Length); 
         Assert.Equal(buffer.WrittenSpan.ToArray(),expectedOutput);
      }
      
      [Theory]
      [InlineData(Bolt8TestVectorParameters.ActOne.INITIATOR_OUTPUT,
         Bolt8TestVectorParameters.ActThree.INITIATOR_OUTPUT)]
      public void TestHandshakeActThreeResponderSide(string actOneInput,string actThreeInput)
      {
         _noiseProtocol = GetResponderNoiseProtocol();
      
         var buffer = new ArrayBufferWriter<byte>();
         
         //read
         _noiseProtocol.Handshake(actOneInput.ToByteArray(),buffer);
         buffer.Clear();
         
         //write
         _noiseProtocol.Handshake(actThreeInput.ToByteArray(),buffer);

         Assert.Equal(buffer.WrittenCount, 0);
         Assert.Empty(buffer.WrittenSpan.ToArray());
      }
      
      private class PredefinedKeysNodeContext : NodeContext
      {
         public PredefinedKeysNodeContext(IRandomNumberGenerator randomNumberGenerator,
            byte[] privateKey) 
            : base(randomNumberGenerator)
         {
            PrivateKey = privateKey;
         }
      }
      
      private class InitiatorTestHandshakeStateFactory : IHandshakeStateFactory
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
            
            handshake?.SetDh(new DhWrapperWithDefinedEphemeralKey(Bolt8TestVectorParameters.InitiatorEphemeralKeyPair));

            return handshake;
         }
      }
      
      private class ResponderTestHandshakeStateFactory : IHandshakeStateFactory
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
            
            handshake?.SetDh(new DhWrapperWithDefinedEphemeralKey(Bolt8TestVectorParameters.ResponderEphemeralKeyPair));

            return handshake;
         }
      }
   }
}