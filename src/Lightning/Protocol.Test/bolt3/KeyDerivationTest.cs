using Bitcoin.Primitives.Fundamental;
using Microsoft.Extensions.Logging;
using Moq;
using Protocol.Channels;
using Xunit;

namespace Protocol.Test.bolt3
{
   public class KeyDerivationTest
   {
      [Fact]
      public void TestKeyDerivation()
      {
         var keyDerivation = new KeyDerivation(new Mock<ILogger<KeyDerivation>>().Object);

         var baseSecret = new PrivateKey(Hex.FromString("0x000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f"));
         var perCommitmentSecret = new PrivateKey(Hex.FromString("0x1f1e1d1c1b1a191817161514131211100f0e0d0c0b0a09080706050403020100"));

         PublicKey perCommitmentPoint = keyDerivation.PublicKeyFromPrivateKey(perCommitmentSecret);
         PublicKey basePoint = keyDerivation.PublicKeyFromPrivateKey(baseSecret);

         PublicKey pubkey = keyDerivation.DerivePublickey(basePoint, perCommitmentPoint);
         PrivateKey privkey = keyDerivation.DerivePrivatekey(baseSecret, basePoint, perCommitmentPoint);

         PublicKey pubkey2 = keyDerivation.PublicKeyFromPrivateKey(privkey);
         Assert.Equal(pubkey.GetSpan().ToArray(), pubkey2.GetSpan().ToArray());

         pubkey = keyDerivation.DeriveRevocationPublicKey(basePoint, perCommitmentPoint);
         privkey = keyDerivation.DeriveRevocationPrivatekey(basePoint, baseSecret, perCommitmentSecret, perCommitmentPoint);

         pubkey2 = keyDerivation.PublicKeyFromPrivateKey(privkey);
         Assert.Equal(pubkey.GetSpan().ToArray(), pubkey2.GetSpan().ToArray());
      }
   }
}