using System;
using System.Linq;
using Bitcoin.Primitives.Fundamental;
using Bitcoin.Primitives.Serialization.Serializers;
using Bitcoin.Primitives.Types;
using Protocol.Channels;
using Xunit;

namespace Protocol.Test.bolt3
{
   public class Bolt3AppendixCTestContext
   {
      public ulong funding_amount;
      public ulong dust_limit;
      public ushort to_self_delay;
      public PrivateKey local_funding_privkey;
      public PrivateKey remote_funding_privkey;
      public Secret local_payment_basepoint_secret;
      public Secret remote_payment_basepoint_secret;
      public Secret local_htlc_basepoint_secret;
      public Secret remote_htlc_basepoint_secret;
      public Secret local_per_commitment_secret;
      public Secret local_delayed_payment_basepoint_secret;
      public Secret remote_revocation_basepoint_secret;
      public PrivateKey local_htlcsecretkey;
      public PrivateKey remote_htlcsecretkey;
      public PrivateKey local_delayed_secretkey;
      public PublicKey local_funding_pubkey;
      public PublicKey remote_funding_pubkey;
      public PublicKey local_payment_basepoint;
      public PublicKey remote_payment_basepoint;
      public PublicKey local_htlc_basepoint;
      public PublicKey remote_htlc_basepoint;
      public PublicKey local_delayed_payment_basepoint;
      public PublicKey remote_revocation_basepoint;
      public PublicKey local_per_commitment_point;
      public PublicKey localkey;
      public PublicKey remotekey;
      public PublicKey local_htlckey;
      public PublicKey remote_htlckey;
      public PublicKey local_delayedkey;
      public PublicKey remote_revocation_key;
      public Keyset keyset;
      public uint funding_output_index;
      public ulong commitment_number;
      public ulong cn_obscurer;

      public bool option_anchor_outputs;

      public UInt256 funding_txid;
      public OutPoint funding_tx_outpoint;

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

         local_funding_privkey = new Secret(Hex.FromString("30ff4956bbdd3222d44cc5e8a1261dab1e07957bdac5ae88fe3261ef321f374901").Take(32).ToArray());
         remote_funding_privkey = new Secret(Hex.FromString("1552dfba4f6cf29a62a0af13c8d6981d36d0ef8d61ba10fb0fe90da7634d7e1301").Take(32).ToArray());

         local_per_commitment_secret = new Secret(Hex.FromString("1f1e1d1c1b1a191817161514131211100f0e0d0c0b0a09080706050403020100"));
         local_payment_basepoint_secret = new Secret(Hex.FromString("1111111111111111111111111111111111111111111111111111111111111111"));
         remote_revocation_basepoint_secret = new Secret(Hex.FromString("2222222222222222222222222222222222222222222222222222222222222222"));
         local_delayed_payment_basepoint_secret = new Secret(Hex.FromString("3333333333333333333333333333333333333333333333333333333333333333"));
         remote_payment_basepoint_secret = new Secret(Hex.FromString("4444444444444444444444444444444444444444444444444444444444444444"));

         local_delayed_payment_basepoint = keyDerivation.PublicKeyFromPrivateKey(local_delayed_payment_basepoint_secret);
         local_per_commitment_point = keyDerivation.PublicKeyFromPrivateKey(local_per_commitment_secret);

         local_delayed_secretkey = keyDerivation.DerivePrivatekey(local_delayed_payment_basepoint_secret, local_delayed_payment_basepoint, local_per_commitment_point);

         remote_revocation_basepoint = keyDerivation.PublicKeyFromPrivateKey(remote_revocation_basepoint_secret);
         local_per_commitment_point = keyDerivation.PublicKeyFromPrivateKey(local_per_commitment_secret);
         remote_revocation_key = keyDerivation.DeriveRevocationPublicKey(remote_revocation_basepoint, local_per_commitment_point);

         local_delayedkey = keyDerivation.PublicKeyFromPrivateKey(local_delayed_secretkey);
         local_payment_basepoint = keyDerivation.PublicKeyFromPrivateKey(local_payment_basepoint_secret);

         remote_payment_basepoint = keyDerivation.PublicKeyFromPrivateKey(remote_payment_basepoint_secret);

         // TODO: thjis comment comes from c-lightning dan to investigate:
         /* FIXME: BOLT should include separate HTLC keys */
         local_htlc_basepoint = local_payment_basepoint;
         remote_htlc_basepoint = remote_payment_basepoint;
         local_htlc_basepoint_secret = local_payment_basepoint_secret;
         remote_htlc_basepoint_secret = remote_payment_basepoint_secret;

         remote_htlcsecretkey = keyDerivation.DerivePrivatekey(remote_htlc_basepoint_secret, remote_htlc_basepoint, local_per_commitment_point);

         localkey = keyDerivation.DerivePublickey(local_payment_basepoint, local_per_commitment_point);

         remotekey = keyDerivation.DerivePublickey(remote_payment_basepoint, local_per_commitment_point);

         local_htlcsecretkey = keyDerivation.DerivePrivatekey(local_htlc_basepoint_secret, local_payment_basepoint, local_per_commitment_point);

         local_htlckey = keyDerivation.PublicKeyFromPrivateKey(local_htlcsecretkey);
         remote_htlckey = keyDerivation.DerivePublickey(remote_htlc_basepoint, local_per_commitment_point);

         local_funding_pubkey = keyDerivation.PublicKeyFromPrivateKey(local_funding_privkey);

         remote_funding_pubkey = keyDerivation.PublicKeyFromPrivateKey(remote_funding_privkey);

         cn_obscurer = scripts.CommitNumberObscurer(local_payment_basepoint, remote_payment_basepoint);

         // dotnet has no uint48 types so we use ulong instead, however ulong (which is uint64) has two
         // more bytes in the array then just drop the last to bytes form the array to compute the hex
         Assert.Equal("0x2bb038521914", Hex.ToString(BitConverter.GetBytes(cn_obscurer).Reverse().ToArray().AsSpan().Slice(2)));

         keyset.self_revocation_key = remote_revocation_key;
         keyset.self_delayed_payment_key = local_delayedkey;
         keyset.self_payment_key = localkey;
         keyset.other_payment_key = remotekey;
         keyset.self_htlc_key = local_htlckey;
         keyset.other_htlc_key = remote_htlckey;
      }
   }
}