#nullable enable
using System;
using System.Buffers;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using MithrilShards.Core;
using MithrilShards.Core.Utils;
using Moq;
using Network.Protocol.Transport;
using NoiseProtocol;
using Xunit;

namespace Network.Test.Protocol.Transport
{
   public class HandshakeNoiseProtocolTests
   {
      private HandshakeWithNoiseProtocol? _noiseProtocol;

      private static HandshakeWithNoiseProtocol GetInitiatorNoiseProtocol() =>
         new HandshakeWithNoiseProtocol(new PredefinedKeysNodeContext(
               new DefaultRandomNumberGenerator(), Bolt8TestVectorParameters.Initiator.PrivateKey)
            , Bolt8TestVectorParameters.Responder.PublicKey, NewNoiseProtocol(new FixedKeysGenerator(
                  Bolt8TestVectorParameters.InitiatorEphemeralKeyPair.PrivateKey,
                  Bolt8TestVectorParameters.InitiatorEphemeralKeyPair.PublicKey)
               .AddKeys(Bolt8TestVectorParameters.Initiator.PrivateKey,
                  Bolt8TestVectorParameters.Initiator.PublicKey)));

      private static HandshakeWithNoiseProtocol GetResponderNoiseProtocol() =>
         new HandshakeWithNoiseProtocol(new PredefinedKeysNodeContext(
               new DefaultRandomNumberGenerator(), Bolt8TestVectorParameters.Responder.PrivateKey)
            , null, NewNoiseProtocol(new FixedKeysGenerator(
                  Bolt8TestVectorParameters.ResponderEphemeralKeyPair.PrivateKey,
                  Bolt8TestVectorParameters.ResponderEphemeralKeyPair.PublicKey)
               .AddKeys(Bolt8TestVectorParameters.Responder.PrivateKey,
                  Bolt8TestVectorParameters.Responder.PublicKey)));

      static HandshakeProcessor NewNoiseProtocol(IKeyGenerator keyGenerator) =>
         new HandshakeProcessor(new EllipticCurveActions(), new Hkdf(new HashWithState(), new HashWithState()), 
            new ChaCha20Poly1305CipherFunction(new Mock<ILogger<ChaCha20Poly1305CipherFunction>>().Object)
            , keyGenerator, new NoiseProtocol.Sha256(new Mock<ILogger<NoiseProtocol.Sha256>>().Object),
            new NoiseMessageTransformer(new Hkdf(new HashWithState(), new HashWithState()),
               new ChaCha20Poly1305CipherFunction(new Mock<ILogger<ChaCha20Poly1305CipherFunction>>().Object),
               new ChaCha20Poly1305CipherFunction(new Mock<ILogger<ChaCha20Poly1305CipherFunction>>().Object),
               new Mock<ILogger<NoiseMessageTransformer>>().Object),new Mock<ILogger<HandshakeProcessor>>().Object);

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

      private class PredefinedKeysNodeContext : NodeContext
      {
         public PredefinedKeysNodeContext(IRandomNumberGenerator randomNumberGenerator,
            byte[] privateKey)
            : base(randomNumberGenerator)
         {
            PrivateKey = privateKey;
         }
      }
      
      private class FixedKeysGenerator : IKeyGenerator
      {
         readonly byte[] _privateKey;
         readonly Dictionary<string, byte[]> _keys;

         public FixedKeysGenerator(byte[] privateKey, byte[] publicKey)
         {
            _privateKey = privateKey;
            _keys = new Dictionary<string, byte[]> {{privateKey.ToHexString(), publicKey}};
         }

         public FixedKeysGenerator AddKeys(byte[] privateKey, byte[] publicKey)
         {
            _keys.Add(privateKey.ToHexString(),publicKey);
            return this;
         }

         public byte[] GenerateKey() => _privateKey;

         public ReadOnlySpan<byte> GetPublicKey(byte[] privateKey) =>
            _keys[privateKey.ToHexString()];
      }
   }
}