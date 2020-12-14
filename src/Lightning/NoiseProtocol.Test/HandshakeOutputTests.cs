using System;
using System.Buffers;
using Network.Test.Protocol.Transport.Noise;
using Xunit;

namespace NoiseProtocol.Test
{
   public class HandshakeOutputTests : Bolt8InitiatedNoiseProtocolTests
   {
      // private NoiseProtocol _noiseProtocol;
      //
      // private void WithInitiatorHandshakeInitiatedToKnownLocalAndRemoteKeys()
      // {
      //    _noiseProtocol = NoiseProtocol(new FixedKeysGenerator(
      //          Bolt8TestVectorParameters.InitiatorEphemeralKeyPair.PrivateKey,
      //          Bolt8TestVectorParameters.InitiatorEphemeralKeyPair.PublicKey)
      //          .AddKeys(Bolt8TestVectorParameters.Initiator.PrivateKey,
      //             Bolt8TestVectorParameters.Initiator.PublicKey),
      //       Bolt8TestVectorParameters.Initiator.PrivateKey);
      //    
      //    _noiseProtocol.InitHandShake();
      // }
      //
      // private void WithResponderHandshakeInitiatedToKnownLocalKeys()
      // {
      //    _noiseProtocol = NoiseProtocol(new FixedKeysGenerator(
      //          Bolt8TestVectorParameters.ResponderEphemeralKeyPair.PrivateKey,
      //          Bolt8TestVectorParameters.ResponderEphemeralKeyPair.PublicKey)
      //          .AddKeys(Bolt8TestVectorParameters.Responder.PrivateKey,
      //             Bolt8TestVectorParameters.Responder.PublicKey),
      //       Bolt8TestVectorParameters.Responder.PrivateKey);
      //    
      //    _noiseProtocol.InitHandShake();
      // }
      //
      // private static NoiseProtocol NoiseProtocol(IKeyGenerator keyGenerator, byte[] s)
      // {
      //    return new NoiseProtocol(new EllipticCurveActions(), new OldHkdf(new OldHash(), new OldHash()),
      //       new ChaCha20Poly1305CipherFunction(), keyGenerator, new HashFunction(),
      //       s);
      // }

      private void WithInitiatorActOneCompletedSuccessfully()
      {
         IBufferWriter<byte> buffer = new ArrayBufferWriter<byte>(50);

         NoiseProtocol.StartNewInitiatorHandshake(Bolt8TestVectorParameters.Responder.PublicKey, buffer);
      }

      [Theory]
      [InlineData(Bolt8TestVectorParameters.ActOne.END_STATE_HASH, Bolt8TestVectorParameters.ActOne.INITIATOR_OUTPUT)]
      public void ActOneInitiatorOutputFitsLightningNetworkBolt8testVector(string expectedHashHex, string expectedOutputHex)
      {
         WithInitiatorHandshakeInitiatedToKnownLocalAndRemoteKeys();

         var buffer = new ArrayBufferWriter<byte>(50);

         NoiseProtocol.StartNewInitiatorHandshake(Bolt8TestVectorParameters.Responder.PublicKey, buffer);

         var expectedOutput = expectedOutputHex.ToByteArray();

         Assert.Equal(buffer.WrittenCount, expectedOutput.Length);
         Assert.Equal(buffer.WrittenSpan.ToArray(), expectedOutput);
         Assert.Equal(expectedHashHex.ToByteArray(), NoiseProtocol.HandshakeContext.Hash);
      }


      [Theory]
      [InlineData(Bolt8TestVectorParameters.ActOne.INITIATOR_OUTPUT,
          Bolt8TestVectorParameters.ActTwo.END_STATE_HASH,
          Bolt8TestVectorParameters.ActTwo.RESPONDER_OUTPUT)]
      public void ActTwoResponderSide(string actOneValidInput, string expectedHashHex, string expectedOutputHex)
      {
         WithResponderHandshakeInitiatedToKnownLocalKeys();

         var buffer = new ArrayBufferWriter<byte>(50);

         NoiseProtocol.ProcessHandshakeRequest(actOneValidInput.ToByteArray(), buffer);

         var expectedOutput = expectedOutputHex.ToByteArray();

         Assert.Equal(expectedOutput.Length, buffer.WrittenCount);
         Assert.Equal(expectedOutput,buffer.WrittenSpan.ToArray());
         Assert.Equal(expectedHashHex.ToByteArray(), NoiseProtocol.HandshakeContext.Hash);
      }

      [Theory]
      [InlineData(Bolt8TestVectorParameters.ActTwo.RESPONDER_OUTPUT,
          Bolt8TestVectorParameters.ActThree.END_STATE_HASH,
          Bolt8TestVectorParameters.ActThree.INITIATOR_OUTPUT)]
      public void ActThreeInitiatorSide(string validInputHex, string expectedHashHex, string expectedOutputHex)
      {
         WithInitiatorHandshakeInitiatedToKnownLocalAndRemoteKeys();

         WithInitiatorActOneCompletedSuccessfully();

         var buffer = new ArrayBufferWriter<byte>();

         NoiseProtocol.ProcessHandshakeRequest(validInputHex.ToByteArray(), buffer);
         
         var expectedOutput = expectedOutputHex.ToByteArray();

         Assert.Equal(expectedOutput.Length, buffer.WrittenCount);
         Assert.Equal(expectedHashHex.ToByteArray(),NoiseProtocol.HandshakeContext.Hash);
         Assert.Equal(buffer.WrittenSpan.ToArray(), expectedOutput);
      }
   }
}