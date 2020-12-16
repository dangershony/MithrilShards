using Microsoft.Extensions.Logging;
using Moq;
using Network.Test.Protocol.Transport.Noise;

namespace NoiseProtocol.Test
{
   public class Bolt8InitiatedNoiseProtocolTests
   {
      protected NoiseProtocol NoiseProtocol;

      protected void WithInitiatorHandshakeInitiatedToKnownLocalAndRemoteKeys()
      {
         NoiseProtocol = InitiateNoiseProtocol(new FixedKeysGenerator(
                  Bolt8TestVectorParameters.InitiatorEphemeralKeyPair.PrivateKey,
                  Bolt8TestVectorParameters.InitiatorEphemeralKeyPair.PublicKey)
               .AddKeys(Bolt8TestVectorParameters.Initiator.PrivateKey,
                  Bolt8TestVectorParameters.Initiator.PublicKey),
            Bolt8TestVectorParameters.Initiator.PrivateKey);
         
         NoiseProtocol.InitHandShake(Bolt8TestVectorParameters.Initiator.PrivateKey);
      }

      protected void WithResponderHandshakeInitiatedToKnownLocalKeys()
      {
         NoiseProtocol = InitiateNoiseProtocol(new FixedKeysGenerator(
                  Bolt8TestVectorParameters.ResponderEphemeralKeyPair.PrivateKey,
                  Bolt8TestVectorParameters.ResponderEphemeralKeyPair.PublicKey)
               .AddKeys(Bolt8TestVectorParameters.Responder.PrivateKey,
                  Bolt8TestVectorParameters.Responder.PublicKey),
            Bolt8TestVectorParameters.Responder.PrivateKey);
         
         NoiseProtocol.InitHandShake(Bolt8TestVectorParameters.Responder.PrivateKey);
      }

      internal static NoiseProtocol InitiateNoiseProtocol(IKeyGenerator keyGenerator, byte[] s)
      {
         var hkdf = new OldHkdf(new OldHash(), new OldHash());
         
         return new NoiseProtocol(new EllipticCurveActions(), hkdf, new ChaCha20Poly1305CipherFunction(
               new Mock<ILogger<ChaCha20Poly1305CipherFunction>>().Object), keyGenerator, 
               new Sha256(new Mock<ILogger<Sha256>>().Object), new NoiseMessageTransformer(hkdf,
               new ChaCha20Poly1305CipherFunction(new Mock<ILogger<ChaCha20Poly1305CipherFunction>>().Object),
               new ChaCha20Poly1305CipherFunction(new Mock<ILogger<ChaCha20Poly1305CipherFunction>>().Object),
               new Mock<ILogger<NoiseMessageTransformer>>().Object),new Mock<ILogger<NoiseProtocol>>().Object);
      }
   }
}