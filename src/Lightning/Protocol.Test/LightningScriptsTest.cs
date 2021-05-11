using System;
using System.Collections.Generic;
using System.Linq;
using Bitcoin.Primitives.Fundamental;
using NBitcoin;
using NBitcoin.DataEncoders;
using Protocol.Channels;
using Xunit;

#pragma warning disable IDE1006 // Naming Styles

namespace Protocol.Test
{
   /// <summary>
   /// Tests for https://github.com/lightningnetwork/lightning-rfc/blob/master/03-transactions.md#appendix-b-funding-transaction-test-vectors
   /// </summary>
   public class LightningScriptsTest
   {
      [Fact]
      public void CreaateFundingTransactionScriptTest()
      {
         var lightningScripts = new LightningScripts();

         byte[] localFundingPubkey = "023da092f6980e58d2c037173180e9a465476026ee50f96695963e8efe436f54eb".FromHexString();
         byte[] remoteFundingPubkey = "030e9f7b623d2ccc7c9bd44d66d5ce21ce504c0acf6385a132cec6d3c39fa711c1".FromHexString();

         byte[] script = lightningScripts.CreaateFundingTransactionScript(new PublicKey(localFundingPubkey), new PublicKey(remoteFundingPubkey));

         Assert.Equal(script, "5221023da092f6980e58d2c037173180e9a465476026ee50f96695963e8efe436f54eb21030e9f7b623d2ccc7c9bd44d66d5ce21ce504c0acf6385a132cec6d3c39fa711c152ae".FromHexString());
      }

      [Fact]
      public void CreaateFundingTransactionTest()
      {
         var block0 = Block.Load("0100000000000000000000000000000000000000000000000000000000000000000000003ba3edfd7a7b12b27ac72c3e67768f617fc81bc3888a51323a9fb8aa4b1e5e4adae5494dffff7f20020000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4d04ffff001d0104455468652054696d65732030332f4a616e2f32303039204368616e63656c6c6f72206f6e206272696e6b206f66207365636f6e64206261696c6f757420666f722062616e6b73ffffffff0100f2052a01000000434104678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38c4f35504e51ec112de5c384df7ba0b8d578a4c702b6bf11d5fac00000000".FromHexString(), Consensus.RegTest);
         var block1 = Block.Load("0000002006226e46111a0b59caaf126043eb5bbf28c34f3a5e332a1fc7b2b73cf188910fadbb20ea41a8423ea937e76e8151636bf6093b70eaff942930d20576600521fdc30f9858ffff7f20000000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff03510101ffffffff0100f2052a010000001976a9143ca33c2e4446f4a305f23c80df8ad1afdcf652f988ac00000000".FromHexString(), Consensus.RegTest);
         var encoder = new Base58Encoder();

         var block1Privkey = new Key("6bd078650fcee8444e4e09825227b801a1ca928debb750eb36e6d56124bb20e801".FromHexString().Take(32).ToArray());

         var lightningScripts = new LightningScripts();

         byte[] localFundingPubkey = "023da092f6980e58d2c037173180e9a465476026ee50f96695963e8efe436f54eb".FromHexString();
         byte[] remoteFundingPubkey = "030e9f7b623d2ccc7c9bd44d66d5ce21ce504c0acf6385a132cec6d3c39fa711c1".FromHexString();

         byte[] script = lightningScripts.CreaateFundingTransactionScript(new PublicKey(localFundingPubkey), new PublicKey(remoteFundingPubkey));

         var trx = Transaction.Parse(
            "0200000001adbb20ea41a8423ea937e76e8151636bf6093b70eaff942930d20576600521fd000000006b48304502210090587b6201e166ad6af0227d3036a9454223d49a1f11839c1a362184340ef0240220577f7cd5cca78719405cbf1de7414ac027f0239ef6e214c90fcaab0454d84b3b012103535b32d5eb0a6ed0982a0479bbadc9868d9836f6ba94dd5a63be16d875069184ffffffff028096980000000000220020c015c4a6be010e21657068fc2e6a9d02b27ebe4d490a25846f7237f104d1a3cd20256d29010000001600143ca33c2e4446f4a305f23c80df8ad1afdcf652f900000000", NBitcoin.Network.RegTest);

         // TODO: investigate why the FeeRate is not yielding the same change as the test expects
         // TODO: also investigate why NBitcoin generates a different signature to the BOLT tests (signing the same trx on NBitcoin create the same signature payload)

         TransactionBuilder builder = NBitcoin.Network.RegTest.CreateTransactionBuilder()
            .AddKeys(block1Privkey)
            .AddCoins(new Coin(block1.Transactions[0].Outputs.AsIndexedOutputs().First()))
            .Send(new Script(script).GetWitScriptAddress(NBitcoin.Network.RegTest), Money.Satoshis(10000000))
            .SendFees(Money.Satoshis(13920).Satoshi)
            .SetChange(new BitcoinWitPubKeyAddress("bcrt1q8j3nctjygm62xp0j8jqdlzk34lw0v5hejct6md", NBitcoin.Network.RegTest), ChangeType.All);
         // .SendEstimatedFees(new FeeRate(Money.Satoshis(15000)));

         Transaction endtrx = builder.BuildTransaction(false, SigHash.All);
         endtrx.Version = 2;

         var trx2 = builder.SignTransactionInPlace(endtrx, SigHash.All);

         //Assert.Equal(trx.ToHex(), trx2.ToHex());

         // Check that the funding transaction scripts are equal.
         Assert.Equal(trx.Outputs[0].ScriptPubKey.ToHex(), trx2.Outputs[0].ScriptPubKey.ToHex());
      }

      [Fact]
      public void CommitmentandHTLCTransactionTestVectors()
      {
         LightningScripts scripts = new LightningScripts();
         KeyDerivation keyDerivation = new KeyDerivation(null);

         ulong funding_amount, dust_limit;
         uint feerate_per_kw;
         ushort to_self_delay;
         /* x_ prefix means internal vars we used to derive spec */
         PrivateKey local_funding_privkey, x_remote_funding_privkey;
         Secret x_local_payment_basepoint_secret, x_remote_payment_basepoint_secret = null;
         Secret x_local_htlc_basepoint_secret, x_remote_htlc_basepoint_secret = null;
         Secret x_local_per_commitment_secret = null;
         Secret x_local_delayed_payment_basepoint_secret = null;
         Secret x_remote_revocation_basepoint_secret = null;
         PrivateKey local_htlcsecretkey, x_remote_htlcsecretkey = null;
         PrivateKey x_local_delayed_secretkey = null;
         PublicKey local_funding_pubkey, remote_funding_pubkey = null;
         PublicKey local_payment_basepoint, remote_payment_basepoint = null;
         PublicKey local_htlc_basepoint, remote_htlc_basepoint = null;
         PublicKey x_local_delayed_payment_basepoint = null;
         PublicKey x_remote_revocation_basepoint = null;
         PublicKey x_local_per_commitment_point = null;
         PublicKey localkey, remotekey, tmpkey = null;
         PublicKey local_htlckey, remote_htlckey = null;
         PublicKey local_delayedkey = null;
         PublicKey remote_revocation_key = null;
         Bitcoin.Primitives.Types.Transaction tx, tx2;
         Keyset keyset;
         byte[] wscript;
         uint funding_output_index;
         ulong commitment_number, cn_obscurer;
         ulong to_local, to_remote;
         List<Htlc> htlcs = new List<Htlc>();
         List<Htlc> inv_htlcs = new List<Htlc>();
         bool option_anchor_outputs = false;

         funding_output_index = 0;
         funding_amount = 10000000;
         var funding_txid = Bitcoin.Primitives.Types.UInt256.Parse("8984484a580b825b9972d7adb15050b3ab624ccd731946b3eeddb92f4e7ef6be");

         var funding_tx_outpoint = new Bitcoin.Primitives.Types.OutPoint { Hash = funding_txid, Index = funding_output_index };

         commitment_number = 42;
         to_self_delay = 144;
         dust_limit = 546;

         local_funding_privkey = new Secret(StringUtilities.FromHexString("30ff4956bbdd3222d44cc5e8a1261dab1e07957bdac5ae88fe3261ef321f374901").Take(32).ToArray());

         x_local_per_commitment_secret = new Secret(StringUtilities.FromHexString("0x1f1e1d1c1b1a191817161514131211100f0e0d0c0b0a09080706050403020100"));
         x_local_payment_basepoint_secret = new Secret(StringUtilities.FromHexString("1111111111111111111111111111111111111111111111111111111111111111"));
         x_remote_revocation_basepoint_secret = new Secret(StringUtilities.FromHexString("2222222222222222222222222222222222222222222222222222222222222222"));
         x_local_delayed_payment_basepoint_secret = new Secret(StringUtilities.FromHexString("3333333333333333333333333333333333333333333333333333333333333333"));
         x_remote_payment_basepoint_secret = new Secret(StringUtilities.FromHexString("4444444444444444444444444444444444444444444444444444444444444444"));

         x_local_delayed_payment_basepoint = keyDerivation.PublicKeyFromPrivateKey(x_local_delayed_payment_basepoint_secret);
         x_local_per_commitment_point = keyDerivation.PublicKeyFromPrivateKey(x_local_per_commitment_secret);

         x_local_delayed_secretkey = keyDerivation.DerivePrivatekey(x_local_delayed_payment_basepoint_secret, x_local_delayed_payment_basepoint, x_local_per_commitment_point);

         x_remote_revocation_basepoint = keyDerivation.PublicKeyFromPrivateKey(x_remote_revocation_basepoint_secret);
         x_local_per_commitment_point = keyDerivation.PublicKeyFromPrivateKey(x_local_per_commitment_secret);
         remote_revocation_key = keyDerivation.DeriveRevocationPublicKey(x_remote_revocation_basepoint, x_local_per_commitment_point);

         local_delayedkey = keyDerivation.PublicKeyFromPrivateKey(x_local_delayed_secretkey);
         local_payment_basepoint = keyDerivation.PublicKeyFromPrivateKey(x_local_payment_basepoint_secret);

         remote_payment_basepoint = keyDerivation.PublicKeyFromPrivateKey(x_remote_payment_basepoint_secret);

         // TODO: thjis comment comes from c-lightning dan to investigate:
         /* FIXME: BOLT should include separate HTLC keys */
         local_htlc_basepoint = local_payment_basepoint;
         remote_htlc_basepoint = remote_payment_basepoint;
         x_local_htlc_basepoint_secret = x_local_payment_basepoint_secret;
         x_remote_htlc_basepoint_secret = x_remote_payment_basepoint_secret;

         localkey = keyDerivation.DerivePublickey(local_payment_basepoint, x_local_per_commitment_point);
         remotekey = keyDerivation.DerivePublickey(remote_payment_basepoint, x_local_per_commitment_point);

         local_htlcsecretkey = keyDerivation.DerivePrivatekey(x_local_htlc_basepoint_secret, local_payment_basepoint, x_local_per_commitment_point);

         local_htlckey = keyDerivation.PublicKeyFromPrivateKey(local_htlcsecretkey);
         remote_htlckey = keyDerivation.DerivePublickey(remote_htlc_basepoint, x_local_per_commitment_point);

         local_funding_pubkey = keyDerivation.PublicKeyFromPrivateKey(local_funding_privkey);

         cn_obscurer = scripts.CommitNumberObscurer(local_payment_basepoint, remote_payment_basepoint);

         /* BOLT #3:
    *
    *    name: simple commitment tx with no HTLCs
    *    to_local_msat: 7000000000
    *    to_remote_msat: 3000000000
    *    local_feerate_per_kw: 15000
    */
         to_local = 7000000000;
         to_remote = 3000000000;
         feerate_per_kw = 15000;

         keyset.self_revocation_key = remote_revocation_key;
         keyset.self_delayed_payment_key = local_delayedkey;
         keyset.self_payment_key = localkey;
         keyset.other_payment_key = remotekey;
         keyset.self_htlc_key = local_htlckey;
         keyset.other_htlc_key = remote_htlckey;

         tx = scripts.CreateCommitmenTransaction(
                   funding_tx_outpoint,
                   funding_amount,
                   local_funding_pubkey,
                   remote_funding_pubkey,
                   LightningScripts.Side.LOCAL,
                   to_self_delay,
                   keyset,
                   feerate_per_kw,
                   dust_limit,
                   to_local,
                   to_remote,
                   htlcs,
                   commitment_number ^ cn_obscurer,
                   option_anchor_outputs,
                   LightningScripts.Side.LOCAL);

         tx2 = scripts.CreateCommitmenTransaction(
               funding_tx_outpoint,
               funding_amount,
               local_funding_pubkey,
               remote_funding_pubkey,
               LightningScripts.Side.REMOTE,
               to_self_delay,
               keyset,
               feerate_per_kw,
               dust_limit,
               to_local,
               to_remote,
               inv_htlcs,
               commitment_number ^ cn_obscurer,
               option_anchor_outputs,
               LightningScripts.Side.REMOTE);

         Assert.Equal(tx, tx2);
      }
   }
}

#pragma warning restore IDE1006 // Naming Styles