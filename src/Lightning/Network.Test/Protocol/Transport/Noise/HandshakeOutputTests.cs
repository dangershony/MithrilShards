using System.Collections.Generic;
using Network.Protocol.Transport.Noise;
using Xunit;

namespace Network.Test.Protocol.Transport.Noise
{
   public class HandshakeOutputTests
   {
      private HandshakeState<ChaCha20Poly1305, CurveSecp256K1, Sha256> _handshakeState;

      private void WithInitiatorHandshakeInitiatedToKnownLocalAndRemoteKeys()
      {
         _handshakeState = InitiateHandShake(true, Bolt8TestVectorParameters.Initiator.PrivateKey
             , Bolt8TestVectorParameters.Receiver.PublicKey);

         _handshakeState.SetDh(
             new DhWrapperWithDefinedEphemeralKey(Bolt8TestVectorParameters.InitiatorEphemeralKeyPair));
      }

      private void WithResponderHandshakeInitiatedToKnownLocalKeys()
      {
         _handshakeState = InitiateHandShake(false, Bolt8TestVectorParameters.Receiver.PrivateKey);

         _handshakeState.SetDh(
             new DhWrapperWithDefinedEphemeralKey(Bolt8TestVectorParameters.ReceiverEphemeralKeyPair));
      }

      private HandshakeState<ChaCha20Poly1305, CurveSecp256K1, Sha256> InitiateHandShake(bool isInitiator,
          byte[] s = null, byte[] rs = null)
      {
         return new HandshakeState<ChaCha20Poly1305, CurveSecp256K1, Sha256>(
             Network.Protocol.Transport.Noise.Protocol.Parse(LightningNetworkConfig.PROTOCOL_NAME), isInitiator,
             LightningNetworkConfig.ProlugeByteArray(), s, rs, new List<byte[]>(),
             LightningNetworkConfig.NoiseProtocolVersionPrefix);
      }

      private byte[] WithInitiatorActOneCompletedSuccessfully()
      {
         var buffer = new byte[50];

         _handshakeState.WriteMessage(null, buffer);

         return buffer;
      }

      private byte[] WithInitiatorActTwoCompletesSuccessfully(byte[] input)
      {
         var buffer = new byte[50];

         _handshakeState.ReadMessage(input, buffer);
         return buffer;
      }

      private byte[] WithResponderActOneCompletedSuccessfully(byte[] input)
      {
         var buffer = new byte[50];

         _handshakeState.ReadMessage(input, buffer);

         return buffer;
      }

      [Theory]
      [InlineData(Bolt8TestVectorParameters.ActOne.EndStateHash, Bolt8TestVectorParameters.ActOne.InitiatorOutput)]
      public void ActOneOutputFitsLightningNetworkBolt8testVector(string expectedHashHex, string expectedOutputHex)
      {
         WithInitiatorHandshakeInitiatedToKnownLocalAndRemoteKeys();

         var buffer = new byte[50];

         var (ciphertextSize, handshakeHash, transport) = _handshakeState.WriteMessage(null, buffer);

         var expectedOutput = expectedOutputHex.ToByteArray();

         Assert.Equal(ciphertextSize, expectedOutput.Length);
         Assert.Equal(expectedHashHex.ToByteArray(), handshakeHash);
         Assert.Null(transport);
         Assert.Equal(buffer, expectedOutput);
      }

      [Theory]
      [InlineData(Bolt8TestVectorParameters.ActOne.InitiatorOutput, Bolt8TestVectorParameters.ActOne.EndStateHash)]
      public void ActOneResponder(string validInputHex, string expectedHashHex)
      {
         WithResponderHandshakeInitiatedToKnownLocalKeys();

         var buffer = new byte[50];

         var (ciphertextSize, handshakeHash, transport) =
             _handshakeState.ReadMessage(validInputHex.ToByteArray(), buffer);

         Assert.Equal(ciphertextSize, 0);
         Assert.Equal(expectedHashHex.ToByteArray(), handshakeHash);
         Assert.Null(transport);
         Assert.Equal(buffer, new byte[50]);
      }

      [Theory]
      [InlineData(Bolt8TestVectorParameters.ActOne.InitiatorOutput,
          Bolt8TestVectorParameters.ActTwo.EndStateHash,
          Bolt8TestVectorParameters.ActTwo.ResponderOutput)]
      public void ActTwoResponderSide(string actOneValidInput, string expectedHashHex, string expectedOutputHex)
      {
         WithResponderHandshakeInitiatedToKnownLocalKeys();

         WithResponderActOneCompletedSuccessfully(actOneValidInput.ToByteArray());

         var buffer = new byte[50];

         var (ciphertextSize, handshakeHash, transport) = _handshakeState.WriteMessage(null, buffer);

         var expectedOutput = expectedOutputHex.ToByteArray();

         Assert.Equal(ciphertextSize, expectedOutput.Length);
         Assert.Equal(handshakeHash, expectedHashHex.ToByteArray());
         Assert.Null(transport);
         Assert.Equal(buffer, expectedOutput);
      }

      [Theory]
      [InlineData(Bolt8TestVectorParameters.ActTwo.ResponderOutput, Bolt8TestVectorParameters.ActTwo.EndStateHash)]
      public void ActTwoInitiatorSide(string validInputHex, string expectedHashHex)
      {
         WithInitiatorHandshakeInitiatedToKnownLocalAndRemoteKeys();

         WithInitiatorActOneCompletedSuccessfully();

         var buffer = new byte[50];

         var (ciphertextSize, handshakeHash, transport) =
             _handshakeState.ReadMessage(validInputHex.ToByteArray(), buffer);

         Assert.Equal(ciphertextSize, 0);
         Assert.Equal(handshakeHash, expectedHashHex.ToByteArray());
         Assert.Null(transport);
         Assert.Equal(buffer, new byte[50]);
      }

      [Theory]
      [InlineData(Bolt8TestVectorParameters.ActTwo.ResponderOutput,
          Bolt8TestVectorParameters.ActThree.EndStateHash,
          Bolt8TestVectorParameters.ActThree.InitiatorOutput)]
      public void ActThreeInitiatorSide(string validInputHex, string expectedHashHex, string expectedOutputHex)
      {
         WithInitiatorHandshakeInitiatedToKnownLocalAndRemoteKeys();

         WithInitiatorActOneCompletedSuccessfully();

         WithInitiatorActTwoCompletesSuccessfully(validInputHex.ToByteArray());

         var buffer = new byte[66];

         var (ciphertextSize, _, t) = _handshakeState.WriteMessage(null, buffer);

         var expectedOutput = expectedOutputHex.ToByteArray();

         Assert.Equal(ciphertextSize, expectedOutput.Length);
         Assert.NotNull(t);
         Assert.Equal(buffer, expectedOutput);

         var transport = t as Transport<ChaCha20Poly1305>;
         Assert.NotNull(transport);
      }
   }
}