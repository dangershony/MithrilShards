using Bitcoin.Primitives.Fundamental;
using Microsoft.Extensions.Logging;
using Moq;
using Protocol.Channels;
using Xunit;

namespace Protocol.Test.bolt3
{
   public class Bolt3KeyDerivationTest
   {
      [Fact]
      public void AppendixEKeyDerivationTest()
      {
         var keyDerivation = new LightningKeyDerivation(new Mock<ILogger<LightningKeyDerivation>>().Object);

         var baseSecret = new PrivateKey(Hex.FromString("0x000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f"));
         var perCommitmentSecret = new PrivateKey(Hex.FromString("0x1f1e1d1c1b1a191817161514131211100f0e0d0c0b0a09080706050403020100"));

         PublicKey perCommitmentPoint = keyDerivation.PublicKeyFromPrivateKey(perCommitmentSecret);
         PublicKey basePoint = keyDerivation.PublicKeyFromPrivateKey(baseSecret);

         Assert.Equal("0x025f7117a78150fe2ef97db7cfc83bd57b2e2c0d0dd25eaf467a4a1c2a45ce1486", Hex.ToString(perCommitmentPoint));
         Assert.Equal("0x036d6caac248af96f6afa7f904f550253a0f3ef3f5aa2fe6838a95b216691468e2", Hex.ToString(basePoint));

         PublicKey pubkey = keyDerivation.DerivePublickey(basePoint, perCommitmentPoint);
         PrivateKey privkey = keyDerivation.DerivePrivatekey(baseSecret, basePoint, perCommitmentPoint);

         Assert.Equal("0x0235f2dbfaa89b57ec7b055afe29849ef7ddfeb1cefdb9ebdc43f5494984db29e5", Hex.ToString(pubkey));
         Assert.Equal("0xcbced912d3b21bf196a766651e436aff192362621ce317704ea2f75d87e7be0f", Hex.ToString(privkey));

         PublicKey pubkey2 = keyDerivation.PublicKeyFromPrivateKey(privkey);
         Assert.Equal(pubkey.GetSpan().ToArray(), pubkey2.GetSpan().ToArray());

         pubkey = keyDerivation.DeriveRevocationPublicKey(basePoint, perCommitmentPoint);
         privkey = keyDerivation.DeriveRevocationPrivatekey(basePoint, baseSecret, perCommitmentSecret, perCommitmentPoint);

         Assert.Equal("0x02916e326636d19c33f13e8c0c3a03dd157f332f3e99c317c141dd865eb01f8ff0", Hex.ToString(pubkey));
         Assert.Equal("0xd09ffff62ddb2297ab000cc85bcb4283fdeb6aa052affbc9dddcf33b61078110", Hex.ToString(privkey));

         pubkey2 = keyDerivation.PublicKeyFromPrivateKey(privkey);
         Assert.Equal(pubkey.GetSpan().ToArray(), pubkey2.GetSpan().ToArray());
      }
   }
}