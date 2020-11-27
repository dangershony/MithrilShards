using Network.Protocol.Transport.Noise;
using Xunit;

namespace Network.Test.Protocol.Transport.Noise
{
   public class ProtocolTests
   {
      private Network.Protocol.Transport.Noise.Protocol _protocol;

      [Fact]
      public void ParsingTheLightningStringCipherToChaChaPoly()
      {
         _protocol = Network.Protocol.Transport.Noise.Protocol.Parse(LightningNetworkConfig.PROTOCOL_NAME);

         Assert.Equal(CipherFunction.ChaChaPoly, _protocol.Cipher);
      }

      [Fact]
      public void ParsingTheLightningStringDhToCurveSecp256K1()
      {
         _protocol = Network.Protocol.Transport.Noise.Protocol.Parse(LightningNetworkConfig.PROTOCOL_NAME);

         Assert.Equal(DhFunction.CurveSecp256K1, _protocol.Dh);
      }

      [Fact]
      public void ParsingTheLightningStringHashToSha256()
      {
         _protocol = Network.Protocol.Transport.Noise.Protocol.Parse(LightningNetworkConfig.PROTOCOL_NAME);

         Assert.Equal(HashFunction.Sha256, _protocol.Hash);
      }

      [Fact]
      public void ParsingTheLightningStringHandshakePatternToXk()
      {
         _protocol = Network.Protocol.Transport.Noise.Protocol.Parse(LightningNetworkConfig.PROTOCOL_NAME);

         Assert.Equal(HandshakePattern.XK, _protocol.HandshakePattern);
      }
   }
}