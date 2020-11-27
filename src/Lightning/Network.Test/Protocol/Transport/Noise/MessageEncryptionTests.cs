using System;
using System.Collections.Generic;
using System.Linq;
using Network.Protocol.Transport.Noise;
using Xunit;
using Xunit.Abstractions;

namespace Network.Test.Protocol.Transport.Noise
{
   public class MessageEncryptionTests
   {
      private readonly ITestOutputHelper _testOutputHelper;
      private HandshakeState<ChaCha20Poly1305, CurveSecp256K1, Sha256> _initiatorHandshakeState;
      private HandshakeState<ChaCha20Poly1305, CurveSecp256K1, Sha256> _responderHandshakeState;

      public MessageEncryptionTests(ITestOutputHelper testOutputHelper)
      {
         _testOutputHelper = testOutputHelper;
      }

      private void WithInitiatorHandshakeInitiatedToKnownLocalAndRemoteKeys()
      {
         _initiatorHandshakeState = InitiateHandShake(true, Bolt8TestVectorParameters.Initiator.PrivateKey
             , Bolt8TestVectorParameters.Responder.PublicKey);

         _initiatorHandshakeState.SetDh(
             new DhWrapperWithDefinedEphemeralKey(Bolt8TestVectorParameters.InitiatorEphemeralKeyPair));
      }

      private void WithResponderHandshakeInitiatedToKnownLocalKeys()
      {
         _responderHandshakeState = InitiateHandShake(false, Bolt8TestVectorParameters.Responder.PrivateKey);

         _responderHandshakeState.SetDh(
             new DhWrapperWithDefinedEphemeralKey(Bolt8TestVectorParameters.ResponderEphemeralKeyPair));
      }

      private HandshakeState<ChaCha20Poly1305, CurveSecp256K1, Sha256> InitiateHandShake(bool isInitiator,
          byte[] s = null, byte[] rs = null)
      {
         return new HandshakeState<ChaCha20Poly1305, CurveSecp256K1, Sha256>(
             Network.Protocol.Transport.Noise.Protocol.Parse(LightningNetworkConfig.PROTOCOL_NAME), isInitiator,
             LightningNetworkConfig.ProlugeByteArray(), s, rs, new List<byte[]>(),
             new byte[] { 0x00 });
      }

      [Theory]
      [InlineData("0xcf2b30ddf0cf3f80e7c35a6e6730b59fe802473180f396d88a8fb0db8cbcf25d2f214cf9ea1d95")]
      public void TestMessageEncryptionIterationZero(string expectedOutputHex)
      {
         var (initiatorTransport, _) = WithTheHandshakeCompletedSuccessfully();

         const string message = "0x68656c6c6f";

         var expectedOutput = EncryptMessage(message.ToByteArray(), initiatorTransport);

         Assert.Equal(expectedOutput, expectedOutputHex.ToByteArray());
      }

      [Theory]
      [InlineData("0x72887022101f0b6753e0c7de21657d35a4cb2a1f5cde2650528bbc8f837d0f0d7ad833b1a256a1")]
      public void TestMessageEncryptionIterationOne(string expectedOutputHex)
      {
         var (initiatorTransport, responderTransport) = WithTheHandshakeCompletedSuccessfully();

         const string message = "0x68656c6c6f";

         var input = EncryptMessage(message.ToByteArray(), initiatorTransport);

         var decryptedMessage = DecryptAndValidateMessage(input, responderTransport);

         var output = EncryptMessage(message.ToByteArray(), initiatorTransport);

         Assert.Equal(decryptedMessage.ToArray(), message.ToByteArray());
         Assert.Equal(output, expectedOutputHex.ToByteArray());
      }

      [Theory]
      [InlineData(500, "0x178cb9d7387190fa34db9c2d50027d21793c9bc2d40b1e14dcf30ebeeeb220f48364f7a4c68bf8")]
      [InlineData(501, "0x1b186c57d44eb6de4c057c49940d79bb838a145cb528d6e8fd26dbe50a60ca2c104b56b60e45bd")]
      [InlineData(1000, "0x4a2f3cc3b5e78ddb83dcb426d9863d9d9a723b0337c89dd0b005d89f8d3c05c52b76b29b740f09")]
      [InlineData(1001, "0x2ecd8c8a5629d0d02ab457a0fdd0f7b90a192cd46be5ecb6ca570bfc5e268338b1a16cf4ef2d36")]
      public void TestMessageEncryptionIterationN(int iterationTarget, string expectedOutputHex)
      {
         var (initiatorTransport, responderTransport) = WithTheHandshakeCompletedSuccessfully();

         const string message = "0x68656c6c6f";

         Span<byte> encryptedMessage = stackalloc byte[39]; //the message that is encrypted is 39 bytes should be protocol max length
         ReadOnlySpan<byte> decryptedMessage = null;

         for (var i = 0; i <= iterationTarget; i++)
         {
            EncryptMessage(message.ToByteArray(), initiatorTransport, encryptedMessage);

            decryptedMessage = DecryptAndValidateMessage(encryptedMessage.ToArray(), responderTransport);
         }

         Assert.Equal(decryptedMessage.ToArray(), message.ToByteArray());
         Assert.Equal(encryptedMessage.ToArray(), expectedOutputHex.ToByteArray());
      }

      private static byte[] EncryptMessage(ReadOnlySpan<byte> m, ITransport transport)
      {
         var outputBuffer = new byte[Network.Protocol.Transport.Noise.Protocol.MAX_MESSAGE_LENGTH];

         var l = BitConverter.GetBytes(Convert.ToInt16(m.Length))
             .Reverse(); //from little endian

         var lengthOfHeader = transport.WriteMessage(l.ToArray(), outputBuffer);

         var lengthOfBody = transport.WriteMessage(m, outputBuffer.AsSpan(lengthOfHeader));

         if (transport.GetNumberOfInitiatorMessages() > 999)
            transport.KeyRecycleInitiatorToResponder();

         return outputBuffer.AsSpan(0, lengthOfHeader + lengthOfBody).ToArray();
      }

      private static void EncryptMessage(ReadOnlySpan<byte> m, ITransport transport, Span<byte> outputBuffer)
      {
         var l = BitConverter.GetBytes(Convert.ToInt16(m.Length))
             .Reverse(); //from little endian

         var lengthOfHeader = transport.WriteMessage(l.ToArray(), outputBuffer);

         transport.WriteMessage(m, outputBuffer.Slice(lengthOfHeader));

         if (transport.GetNumberOfInitiatorMessages() > 999)
            transport.KeyRecycleInitiatorToResponder();
      }

      private static ReadOnlySpan<byte> DecryptAndValidateMessage(ReadOnlySpan<byte> message, ITransport transport)
      {
         var header = new byte[2];

         transport.ReadMessage(message.Slice(0, 18), header);

         var body = new byte[BitConverter.ToInt16(header.Reverse().ToArray())];

         var bodyLength = transport.ReadMessage(message.Slice(18), body);

         if (transport.GetNumberOfInitiatorMessages() > 999)
            transport.KeyRecycleInitiatorToResponder();

         return body.AsSpan(0, bodyLength);
      }

      private (ITransport, ITransport) WithTheHandshakeCompletedSuccessfully()
      {
         WithInitiatorHandshakeInitiatedToKnownLocalAndRemoteKeys();
         WithResponderHandshakeInitiatedToKnownLocalKeys();

         var actOneBuffer = new byte[50];
         _initiatorHandshakeState.WriteMessage(null, actOneBuffer);
         _responderHandshakeState.ReadMessage(actOneBuffer, new byte[50]);

         var actTwoBuffer = new byte[50];
         _responderHandshakeState.WriteMessage(null, actTwoBuffer);
         _initiatorHandshakeState.ReadMessage(actTwoBuffer, new byte[50]);

         var actThreeBuffer = new byte[66];
         var (_, _, initiatorTransport) = _initiatorHandshakeState.WriteMessage(null, actThreeBuffer);
         var (_, _, responderTransport) = _responderHandshakeState.ReadMessage(actThreeBuffer, new byte[66]);

         return (initiatorTransport, responderTransport);
      }
   }
}