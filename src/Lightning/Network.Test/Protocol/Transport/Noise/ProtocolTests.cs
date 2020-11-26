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
         this._protocol = Network.Protocol.Transport.Noise.Protocol.Parse(LightningNetworkConfig.PROTOCOL_NAME);

         Assert.Equal(CipherFunction.ChaChaPoly, this._protocol.Cipher);
      }

      [Fact]
      public void ParsingTheLightningStringDhToCurveSecp256K1()
      {
         this._protocol = Network.Protocol.Transport.Noise.Protocol.Parse(LightningNetworkConfig.PROTOCOL_NAME);

         Assert.Equal(DhFunction.CurveSecp256K1, this._protocol.Dh);
      }

      [Fact]
      public void ParsingTheLightningStringHashToSha256()
      {
         this._protocol = Network.Protocol.Transport.Noise.Protocol.Parse(LightningNetworkConfig.PROTOCOL_NAME);

         Assert.Equal(HashFunction.Sha256, this._protocol.Hash);
      }

      [Fact]
      public void ParsingTheLightningStringHandshakePatternToXk()
      {
         this._protocol = Network.Protocol.Transport.Noise.Protocol.Parse(LightningNetworkConfig.PROTOCOL_NAME);

         Assert.Equal(HandshakePattern.XK, this._protocol.HandshakePattern);
      }
   }
}