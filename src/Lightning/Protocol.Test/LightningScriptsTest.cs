using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Bitcoin.Primitives.Fundamental;
using Bitcoin.Primitives.Serialization;
using Bitcoin.Primitives.Serialization.Serializers;
using Bitcoin.Primitives.Types;
using NBitcoin;
using NBitcoin.DataEncoders;
using Protocol.Channels;
using Protocol.Hashing;
using Xunit;
using Block = NBitcoin.Block;
using Transaction = NBitcoin.Transaction;

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

      [Fact(Skip = "need to sort outputs of the generated trx")]
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

         var trx2 = builder.SignTransactionInPlace(endtrx, new SigningOptions { EnforceLowR = false, SigHash = SigHash.All });

         //Assert.Equal(trx.ToHex(), trx2.ToHex());

         // Check that the funding transaction scripts are equal.
         Assert.Equal(trx.Outputs[0].ScriptPubKey, trx2.Outputs[0].ScriptPubKey);
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
         Bitcoin.Primitives.Types.Transaction tx1, tx2;
         Keyset keyset;
         byte[] wscript;
         uint funding_output_index;
         ulong commitment_number;
         ulong cn_obscurer;
         ulong to_local, to_remote;
         List<Htlc> htlcs;
         List<Htlc> inv_htlcs;
         bool option_anchor_outputs = false;

         htlcs = SetupHtlcs();
         inv_htlcs = InvertHtlcs(htlcs);

         funding_output_index = 0;
         funding_amount = 10000000;
         var funding_txid = Bitcoin.Primitives.Types.UInt256.Parse("8984484a580b825b9972d7adb15050b3ab624ccd731946b3eeddb92f4e7ef6be");

         var funding_tx_outpoint = new Bitcoin.Primitives.Types.OutPoint { Hash = funding_txid, Index = funding_output_index };

         commitment_number = 42;
         to_self_delay = 144;
         dust_limit = 546;

         local_funding_privkey = new Secret(StringUtilities.FromHexString("30ff4956bbdd3222d44cc5e8a1261dab1e07957bdac5ae88fe3261ef321f374901").Take(32).ToArray());
         x_remote_funding_privkey = new Secret(StringUtilities.FromHexString("1552dfba4f6cf29a62a0af13c8d6981d36d0ef8d61ba10fb0fe90da7634d7e1301").Take(32).ToArray());

         x_local_per_commitment_secret = new Secret(StringUtilities.FromHexString("1f1e1d1c1b1a191817161514131211100f0e0d0c0b0a09080706050403020100"));
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

         remote_funding_pubkey = keyDerivation.PublicKeyFromPrivateKey(x_remote_funding_privkey);

         cn_obscurer = scripts.CommitNumberObscurer(local_payment_basepoint, remote_payment_basepoint);

         // dotnet has no uint48 types so we use ulong instead, however ulong (which is uint64) has two
         // more bytes in the array then just drop the last to bytes form the array to compute the hex
         Assert.Equal("0x2bb038521914", StringUtilities.ToHexString(BitConverter.GetBytes(cn_obscurer).Reverse().ToArray().AsSpan().Slice(2)));

         wscript = scripts.FundingRedeemScript(local_funding_pubkey, remote_funding_pubkey);

         string expectedwscript = "5221023da092f6980e58d2c037173180e9a465476026ee50f96695963e8efe436f54eb21030e9f7b623d2ccc7c9bd44d66d5ce21ce504c0acf6385a132cec6d3c39fa711c152ae";
         Assert.Equal(expectedwscript, StringUtilities.ToHexString(wscript.AsSpan()).Substring(2));

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

         List<Htlc> htlcs1 = new List<Htlc>(htlcs);
         List<Htlc> inv_htlcs1 = new List<Htlc>(inv_htlcs);
         htlcs1.Clear();
         inv_htlcs1.Clear();

         tx1 = scripts.CreateCommitmenTransaction(
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
                   htlcs1,
                   commitment_number,
                   cn_obscurer,
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
               inv_htlcs1,
               commitment_number,
               cn_obscurer,
               option_anchor_outputs,
               LightningScripts.Side.REMOTE);

         var transactionSerializer = new TransactionSerializer(new TransactionInputSerializer(new OutPointSerializer(new UInt256Serializer())), new TransactionOutputSerializer(), new TransactionWitnessSerializer(new TransactionWitnessComponentSerializer()));

         var transactionHashCalculator = new TransactionHashCalculator(transactionSerializer);
         tx1.Hash = transactionHashCalculator.ComputeHash(tx1, 1);
         tx2.Hash = transactionHashCalculator.ComputeHash(tx2, 1);

         Assert.Equal(tx1.Hash, tx2.Hash);

         var remote_signature = scripts.SignCommitmentInput(transactionSerializer, tx1, x_remote_funding_privkey, inputIndex: 0, redeemScript: wscript, funding_amount);
         var local_signature = scripts.SignCommitmentInput(transactionSerializer, tx1, local_funding_privkey, inputIndex: 0, redeemScript: wscript, funding_amount);

         string expected_remote_signature = "3045022100f51d2e566a70ba740fc5d8c0f07b9b93d2ed741c3c0860c613173de7d39e7968022041376d520e9c0e1ad52248ddf4b22e12be8763007df977253ef45a4ca3bdb7c0";
         Assert.Equal(expected_remote_signature, StringUtilities.ToHexString(remote_signature.GetSpan().TrimEnd(1)).Substring(2));

         string expected_local_signature = "3044022051b75c73198c6deee1a875871c3961832909acd297c6b908d59e3319e5185a46022055c419379c5051a78d00dbbce11b5b664a0c22815fbcc6fcef6b1937c3836939";
         Assert.Equal(expected_local_signature, StringUtilities.ToHexString(local_signature.GetSpan().TrimEnd(1)).Substring(2));

         var redeemScript = new Script(OpcodeType.OP_0, Op.GetPushOp(local_signature), Op.GetPushOp(remote_signature), Op.GetPushOp(wscript)).ToWitScript();

         tx1.Inputs[0].ScriptWitness = new TransactionWitness
         {
            Components = redeemScript.Pushes.Select(opcode => new TransactionWitnessComponent { RawData = opcode }).ToArray()
         };

         string expected_commitment_transaction = "02000000000101bef67e4e2fb9ddeeb3461973cd4c62abb35050b1add772995b820b584a488489000000000038b02b8002c0c62d0000000000160014ccf1af2f2aabee14bb40fa3851ab2301de84311054a56a00000000002200204adb4e2f00643db396dd120d4e7dc17625f5f2c11a40d857accc862d6b7dd80e0400473044022051b75c73198c6deee1a875871c3961832909acd297c6b908d59e3319e5185a46022055c419379c5051a78d00dbbce11b5b664a0c22815fbcc6fcef6b1937c383693901483045022100f51d2e566a70ba740fc5d8c0f07b9b93d2ed741c3c0860c613173de7d39e7968022041376d520e9c0e1ad52248ddf4b22e12be8763007df977253ef45a4ca3bdb7c001475221023da092f6980e58d2c037173180e9a465476026ee50f96695963e8efe436f54eb21030e9f7b623d2ccc7c9bd44d66d5ce21ce504c0acf6385a132cec6d3c39fa711c152ae3e195220";

         var bytes = StringUtilities.FromHexString(expected_commitment_transaction);
         var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(bytes));
         var expectedtrx = transactionSerializer.Deserialize(ref reader, 1, new ProtocolTypeSerializerOptions((SerializerOptions.SERIALIZE_WITNESS, true)));

         //expectedtrx.Inputs[0].ScriptWitness = null;
         //var buffer1 = new ArrayBufferWriter<byte>();
         //transactionSerializer.Serialize(expectedtrx, 1, buffer1, new ProtocolTypeSerializerOptions((SerializerOptions.SERIALIZE_WITNESS, true)));

         //var reader1 = new SequenceReader<byte>(new ReadOnlySequence<byte>(buffer1.WrittenSpan.ToArray()));
         //var expectedtrx1 = transactionSerializer.Deserialize(ref reader1, 1, new ProtocolTypeSerializerOptions((SerializerOptions.SERIALIZE_WITNESS, true)));

         var buffer = new ArrayBufferWriter<byte>();
         transactionSerializer.Serialize(tx1, 1, buffer, new ProtocolTypeSerializerOptions((SerializerOptions.SERIALIZE_WITNESS, true)));

         Assert.Equal(expected_commitment_transaction, StringUtilities.ToHexString(buffer.WrittenSpan).Substring(2));

         /* BOLT #3:
	       *
	       *    name: commitment tx with all five HTLCs untrimmed (minimum feerate)
	       *    to_local_msat: 6988000000
	       *    to_remote_msat: 3000000000
	       *    local_feerate_per_kw: 0
	       */
         to_local = 6988000000;
         to_remote = 3000000000;
         feerate_per_kw = 0;

         List<Htlc> htlcs2 = new List<Htlc>(htlcs);
         List<Htlc> inv_htlcs2 = new List<Htlc>(inv_htlcs);

         tx1 = scripts.CreateCommitmenTransaction(
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
            htlcs2,
            commitment_number,
            cn_obscurer,
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
            inv_htlcs2,
            commitment_number,
            cn_obscurer,
            option_anchor_outputs,
            LightningScripts.Side.REMOTE);

         tx1.Hash = transactionHashCalculator.ComputeHash(tx1, 1);
         tx2.Hash = transactionHashCalculator.ComputeHash(tx2, 1);

         Assert.Equal(tx1.Hash, tx2.Hash);

         expected_commitment_transaction = "02000000000101bef67e4e2fb9ddeeb3461973cd4c62abb35050b1add772995b820b584a488489000000000038b02b8007e80300000000000022002052bfef0479d7b293c27e0f1eb294bea154c63a3294ef092c19af51409bce0e2ad007000000000000220020403d394747cae42e98ff01734ad5c08f82ba123d3d9a620abda88989651e2ab5d007000000000000220020748eba944fedc8827f6b06bc44678f93c0f9e6078b35c6331ed31e75f8ce0c2db80b000000000000220020c20b5d1f8584fd90443e7b7b720136174fa4b9333c261d04dbbd012635c0f419a00f0000000000002200208c48d15160397c9731df9bc3b236656efb6665fbfe92b4a6878e88a499f741c4c0c62d0000000000160014ccf1af2f2aabee14bb40fa3851ab2301de843110e0a06a00000000002200204adb4e2f00643db396dd120d4e7dc17625f5f2c11a40d857accc862d6b7dd80e04004730440220275b0c325a5e9355650dc30c0eccfbc7efb23987c24b556b9dfdd40effca18d202206caceb2c067836c51f296740c7ae807ffcbfbf1dd3a0d56b6de9a5b247985f060147304402204fd4928835db1ccdfc40f5c78ce9bd65249b16348df81f0c44328dcdefc97d630220194d3869c38bc732dd87d13d2958015e2fc16829e74cd4377f84d215c0b7060601475221023da092f6980e58d2c037173180e9a465476026ee50f96695963e8efe436f54eb21030e9f7b623d2ccc7c9bd44d66d5ce21ce504c0acf6385a132cec6d3c39fa711c152ae3e195220";
         bytes = StringUtilities.FromHexString(expected_commitment_transaction);
         reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(bytes));
         expectedtrx = transactionSerializer.Deserialize(ref reader, 1, new ProtocolTypeSerializerOptions((SerializerOptions.SERIALIZE_WITNESS, true)));
      }

      /* BOLT #3:
    *
    *    htlc 0 direction: remote.local
    *    htlc 0 amount_msat: 1000000
    *    htlc 0 expiry: 500
    *    htlc 0 payment_preimage: 0000000000000000000000000000000000000000000000000000000000000000
    *    htlc 1 direction: remote.local
    *    htlc 1 amount_msat: 2000000
    *    htlc 1 expiry: 501
    *    htlc 1 payment_preimage: 0101010101010101010101010101010101010101010101010101010101010101
    *    htlc 2 direction: local.remote
    *    htlc 2 amount_msat: 2000000
    *    htlc 2 expiry: 502
    *    htlc 2 payment_preimage: 0202020202020202020202020202020202020202020202020202020202020202
    *    htlc 3 direction: local.remote
    *    htlc 3 amount_msat: 3000000
    *    htlc 3 expiry: 503
    *    htlc 3 payment_preimage: 0303030303030303030303030303030303030303030303030303030303030303
    *    htlc 4 direction: remote.local
    *    htlc 4 amount_msat: 4000000
    *    htlc 4 expiry: 504
    *    htlc 4 payment_preimage: 0404040404040404040404040404040404040404040404040404040404040404
    */

      private List<Htlc> SetupHtlcs()
      {
         List<Htlc> htlcs = new List<Htlc>
         {
            new Htlc
            {
               state = htlc_state.RCVD_ADD_ACK_REVOCATION,
               amount = 1000000,
               expirylocktime = 500,
               r = new Preimage(StringUtilities.FromHexString("0000000000000000000000000000000000000000000000000000000000000000")),
            },
            new Htlc
            {
               state = htlc_state.RCVD_ADD_ACK_REVOCATION,
               amount = 2000000,
               expirylocktime = 501,
               r = new Preimage(StringUtilities.FromHexString("0101010101010101010101010101010101010101010101010101010101010101")),
            },
            new Htlc
            {
               state = htlc_state.SENT_ADD_ACK_REVOCATION,
               amount = 2000000,
               expirylocktime = 502,
               r = new Preimage(StringUtilities.FromHexString("0202020202020202020202020202020202020202020202020202020202020202")),
            },
            new Htlc
            {
               state = htlc_state.SENT_ADD_ACK_REVOCATION,
               amount = 3000000,
               expirylocktime = 503,
               r = new Preimage(StringUtilities.FromHexString("0303030303030303030303030303030303030303030303030303030303030303")),
            },
            new Htlc
            {
               state = htlc_state.RCVD_ADD_ACK_REVOCATION,
               amount = 4000000,
               expirylocktime = 504,
               r = new Preimage(StringUtilities.FromHexString("0404040404040404040404040404040404040404040404040404040404040404")),
            },
         };

         foreach (Htlc htlc in htlcs)
         {
            htlc.rhash = new UInt256(HashGenerator.Sha256(htlc.r));
         }

         return htlcs;
      }

      /* HTLCs as seen from other side. */

      private List<Htlc> InvertHtlcs(List<Htlc> htlcs)
      {
         List<Htlc> htlcsinv = new List<Htlc>(htlcs.Count);

         for (var i = 0; i < htlcs.Count; i++)
         {
            Htlc htlc = htlcs[i];

            Htlc inv = new Htlc
            {
               amount = htlc.amount,
               expirylocktime = htlc.expirylocktime,
               id = htlc.id,
               r = htlc.r,
               rhash = htlc.rhash,
               state = htlc.state,
            };

            if (inv.state == htlc_state.RCVD_ADD_ACK_REVOCATION)
            {
               htlc.state = htlc_state.SENT_ADD_ACK_REVOCATION;
            }
            else
            {
               Assert.True(inv.state == htlc_state.SENT_ADD_ACK_REVOCATION);
               htlc.state = htlc_state.RCVD_ADD_ACK_REVOCATION;
            }

            htlcsinv.Add(inv);
         }

         return htlcsinv;
      }
   }
}

#pragma warning restore IDE1006 // Naming Styles