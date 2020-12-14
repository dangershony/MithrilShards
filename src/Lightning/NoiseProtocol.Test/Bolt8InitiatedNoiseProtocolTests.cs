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
         
         NoiseProtocol.InitHandShake();
      }

      protected void WithResponderHandshakeInitiatedToKnownLocalKeys()
      {
         NoiseProtocol = InitiateNoiseProtocol(new FixedKeysGenerator(
                  Bolt8TestVectorParameters.ResponderEphemeralKeyPair.PrivateKey,
                  Bolt8TestVectorParameters.ResponderEphemeralKeyPair.PublicKey)
               .AddKeys(Bolt8TestVectorParameters.Responder.PrivateKey,
                  Bolt8TestVectorParameters.Responder.PublicKey),
            Bolt8TestVectorParameters.Responder.PrivateKey);
         
         NoiseProtocol.InitHandShake();
      }

      internal static NoiseProtocol InitiateNoiseProtocol(IKeyGenerator keyGenerator, byte[] s)
      {
         var hkdf = new OldHkdf(new OldHash(), new OldHash());
         
         return new NoiseProtocol(new EllipticCurveActions(), hkdf, new ChaCha20Poly1305CipherFunction(),
            keyGenerator, new HashFunction(), new NoiseMessageTransformer(hkdf,
               new ChaCha20Poly1305CipherFunction(), new ChaCha20Poly1305CipherFunction() ), s);
      }
   }
}