using System;
using System.Collections.Generic;
using System.Linq;
using Bitcoin.Primitives.Fundamental;
using Bitcoin.Primitives.Serialization.Serializers;
using Bitcoin.Primitives.Types;
using Protocol.Channels;
using Xunit;

namespace Protocol.Test
{
   public class Bolt3AppendixCTestContext
   {
      public ulong funding_amount, dust_limit;
      public ushort to_self_delay;
      /* x_ prefix means internal vars we used to derive spec */
      public PrivateKey local_funding_privkey, x_remote_funding_privkey;
      public Secret x_local_payment_basepoint_secret, x_remote_payment_basepoint_secret;
      public Secret x_local_htlc_basepoint_secret, x_remote_htlc_basepoint_secret;
      public Secret x_local_per_commitment_secret;
      public Secret x_local_delayed_payment_basepoint_secret;
      public Secret x_remote_revocation_basepoint_secret;
      public PrivateKey local_htlcsecretkey, x_remote_htlcsecretkey;
      public PrivateKey x_local_delayed_secretkey;
      public PublicKey local_funding_pubkey, remote_funding_pubkey;
      public PublicKey local_payment_basepoint, remote_payment_basepoint;
      public PublicKey local_htlc_basepoint, remote_htlc_basepoint;
      public PublicKey x_local_delayed_payment_basepoint;
      public PublicKey x_remote_revocation_basepoint;
      public PublicKey x_local_per_commitment_point;
      public PublicKey localkey, remotekey, tmpkey;
      public PublicKey local_htlckey, remote_htlckey;
      public PublicKey local_delayedkey;
      public PublicKey remote_revocation_key;
      public Keyset keyset;
      public byte[] funding_wscript;
      public uint funding_output_index;
      public ulong commitment_number;
      public ulong cn_obscurer;
      public ulong to_local, to_remote;
      public List<Htlc> htlcs;
      public List<Htlc> inv_htlcs;

      public UInt256 funding_txid;
      public Bitcoin.Primitives.Types.OutPoint funding_tx_outpoint;

      public LightningScripts scripts;
      public KeyDerivation keyDerivation;
      public TransactionSerializer transactionSerializer;
      public TransactionHashCalculator transactionHashCalculator;

      public Bolt3AppendixCTestContext()

      {
         scripts = new LightningScripts();
         keyDerivation = new KeyDerivation(null);

         transactionSerializer = new TransactionSerializer(new TransactionInputSerializer(new OutPointSerializer(new UInt256Serializer())), new TransactionOutputSerializer(), new TransactionWitnessSerializer(new TransactionWitnessComponentSerializer()));
         transactionHashCalculator = new TransactionHashCalculator(transactionSerializer);

         funding_output_index = 0;
         funding_amount = 10000000;
         funding_txid = Bitcoin.Primitives.Types.UInt256.Parse("8984484a580b825b9972d7adb15050b3ab624ccd731946b3eeddb92f4e7ef6be");
         funding_tx_outpoint = new Bitcoin.Primitives.Types.OutPoint { Hash = funding_txid, Index = funding_output_index };

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

         funding_wscript = scripts.FundingRedeemScript(local_funding_pubkey, remote_funding_pubkey);

         string expectedwscript = "5221023da092f6980e58d2c037173180e9a465476026ee50f96695963e8efe436f54eb21030e9f7b623d2ccc7c9bd44d66d5ce21ce504c0acf6385a132cec6d3c39fa711c152ae";
         Assert.Equal(expectedwscript, StringUtilities.ToHexString(funding_wscript.AsSpan()).Substring(2));

         keyset.self_revocation_key = remote_revocation_key;
         keyset.self_delayed_payment_key = local_delayedkey;
         keyset.self_payment_key = localkey;
         keyset.other_payment_key = remotekey;
         keyset.self_htlc_key = local_htlckey;
         keyset.other_htlc_key = remote_htlckey;
      }
   }
}