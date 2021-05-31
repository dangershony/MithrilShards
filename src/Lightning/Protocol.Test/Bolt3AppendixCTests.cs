using System;
using System.Collections.Generic;
using Bitcoin.Primitives.Fundamental;
using Bitcoin.Primitives.Types;
using Protocol.Channels;
using Protocol.Hashing;
using Xunit;
using OutPoint = Bitcoin.Primitives.Types.OutPoint;

#pragma warning disable IDE1006 // Naming Styles

namespace Protocol.Test
{
   /// <summary>
   /// Tests for https://github.com/lightningnetwork/lightning-rfc/blob/master/03-transactions.md#appendix-b-funding-transaction-test-vectors
   /// </summary>
   public class Bolt3AppendixCTests : IClassFixture<Bolt3AppendixCTestContext>
   {
      public Bolt3AppendixCTestContext Context { get; set; }

      public Bolt3AppendixCTests(Bolt3AppendixCTestContext context)
      {
         Context = context;
      }

      [Theory]
      [ClassData(typeof(Bolt3AppendixCTestDataStaticRemotekey))]
      public void Bolt3AppendixC_CommitmentAndHTLCTransactionStaticRemotekey(Bolt3AppendixCTestVectors vectors)
      {
         Context.keyset.other_payment_key = Context.remote_payment_basepoint;

         Bolt3AppendixC_CommitmentAndHTLCTransactionTest(vectors);
      }

      [Theory]
      [ClassData(typeof(Bolt3AppendixCTestData))]
      public void Bolt3AppendixC_CommitmentAndHTLCTransaction(Bolt3AppendixCTestVectors vectors)
      {
         Context.keyset.other_payment_key = Context.remotekey;

         Bolt3AppendixC_CommitmentAndHTLCTransactionTest(vectors);
      }

      public void Bolt3AppendixC_CommitmentAndHTLCTransactionTest(Bolt3AppendixCTestVectors vectors)
      {
         CommitmenTransactionOut localCommitmenTransactionOut = Context.scripts.CreateCommitmenTransaction(
                  Context.funding_tx_outpoint,
                  Context.funding_amount,
                  Context.local_funding_pubkey,
                  Context.remote_funding_pubkey,
                  LightningScripts.Side.LOCAL,
                  Context.to_self_delay,
                  Context.keyset,
                  vectors.feerate_per_kw,
                  Context.dust_limit,
                  vectors.to_local_msat,
                  vectors.to_remote_msat,
                  vectors.htlcs_0_to_4.htlcs,
                  Context.commitment_number,
                  Context.cn_obscurer,
                  Context.option_anchor_outputs,
                  LightningScripts.Side.LOCAL);

         var remoteCommitmenTransactionOut = Context.scripts.CreateCommitmenTransaction(
                  Context.funding_tx_outpoint,
                  Context.funding_amount,
                  Context.local_funding_pubkey,
                  Context.remote_funding_pubkey,
                  LightningScripts.Side.REMOTE,
                  Context.to_self_delay,
                  Context.keyset,
                  vectors.feerate_per_kw,
                  Context.dust_limit,
                  vectors.to_local_msat,
                  vectors.to_remote_msat,
                  vectors.htlcs_0_to_4.invertedhtlcs,
                  Context.commitment_number,
                  Context.cn_obscurer,
                  Context.option_anchor_outputs,
                  LightningScripts.Side.REMOTE);

         var localTransaction = localCommitmenTransactionOut.Transaction;
         var remoteTransaction = remoteCommitmenTransactionOut.Transaction;

         localTransaction.Hash = Context.transactionHashCalculator.ComputeHash(localTransaction, 1);
         remoteTransaction.Hash = Context.transactionHashCalculator.ComputeHash(remoteTransaction, 1);

         Assert.Equal(localTransaction.Hash, remoteTransaction.Hash);

         // == helper code==
         var output_commit_tx = TransactionHelper.SeriaizeTransaction(Context.transactionSerializer, Hex.FromString(vectors.output_commit_tx));
         var newtrx = TransactionHelper.ParseToString(localTransaction);
         var curtrx = TransactionHelper.ParseToString(output_commit_tx);

         byte[]? funding_wscript = Context.scripts.FundingRedeemScript(Context.local_funding_pubkey, Context.remote_funding_pubkey);

         var remote_signature = Context.scripts.SignInput(Context.transactionSerializer, localTransaction, Context.remote_funding_privkey, inputIndex: 0, redeemScript: funding_wscript, Context.funding_amount);
         var expected_remote_signature = Hex.ToString(output_commit_tx.Inputs[0].ScriptWitness.Components[2].RawData.AsSpan());
         Assert.Equal(expected_remote_signature, Hex.ToString(remote_signature.GetSpan()));

         var local_signature = Context.scripts.SignInput(Context.transactionSerializer, localTransaction, Context.local_funding_privkey, inputIndex: 0, redeemScript: funding_wscript, Context.funding_amount);
         var expected_local_signature = Hex.ToString(output_commit_tx.Inputs[0].ScriptWitness.Components[1].RawData.AsSpan());
         Assert.Equal(expected_local_signature, Hex.ToString(local_signature.GetSpan()));

         Context.scripts.SetCommitmentInputWitness(localTransaction.Inputs[0], local_signature, remote_signature, funding_wscript);

         byte[] localTransactionBytes = TransactionHelper.DeseriaizeTransaction(Context.transactionSerializer, localTransaction);

         //     var buffer = new ArrayBufferWriter<byte>();
         //   Context.transactionSerializer.Serialize(localTransaction, 1, buffer, new ProtocolTypeSerializerOptions((SerializerOptions.SERIALIZE_WITNESS, true)));

         Assert.Equal(vectors.output_commit_tx, Hex.ToString(localTransactionBytes.AsSpan()).Substring(2));

         /* FIXME: naming here is kind of backwards: local revocation key
          * is derived from remote revocation basepoint, but it's local */

         Keyset keyset = new Keyset
         {
            self_revocation_key = Context.remote_revocation_key,
            self_delayed_payment_key = Context.local_delayedkey,
            self_payment_key = Context.localkey,
            other_payment_key = Context.remotekey,
            self_htlc_key = Context.local_htlckey,
            other_htlc_key = Context.remote_htlckey,
         };

         int htlcOutputIndex = 0;
         for (int htlcIndex = 0; htlcIndex < localCommitmenTransactionOut.Htlcs.Count; htlcIndex++)
         {
            HtlcToOutputMaping htlc = localCommitmenTransactionOut.Htlcs[htlcIndex];

            if (htlc.Htlc == null)
            {
               htlcIndex++;
               continue;
            }

            OutPoint outPoint = new OutPoint { Hash = localTransaction.Hash, Index = (uint)htlcIndex };
            Bitcoin.Primitives.Types.Transaction htlcTransaction;
            byte[] redeemScript;
            if (htlc.Htlc.Side == LightningScripts.Side.LOCAL)
            {
               redeemScript = Context.scripts.GetHtlcOfferedRedeemscript(
                              Context.local_htlckey,
                              Context.remote_htlckey,
                              htlc.Htlc.rhash,
                              Context.remote_revocation_key,
                              Context.option_anchor_outputs);

               htlcTransaction = Context.scripts.CreateHtlcTimeoutTransaction(
                                 Context.option_anchor_outputs,
                                 vectors.feerate_per_kw,
                                 htlc.Htlc.amount,
                                 outPoint,
                                 keyset.self_revocation_key,
                                 keyset.self_delayed_payment_key,
                                 Context.to_self_delay,
                                 (uint)htlc.CltvExpirey);
            }
            else
            {
               redeemScript = Context.scripts.GetHtlcReceivedRedeemscript(
                              htlc.Htlc.expirylocktime,
                              Context.local_htlckey,
                              Context.remote_htlckey,
                              htlc.Htlc.rhash,
                              Context.remote_revocation_key,
                              Context.option_anchor_outputs);

               htlcTransaction = Context.scripts.CreateHtlcSuccessTransaction(
                                 Context.option_anchor_outputs,
                                 vectors.feerate_per_kw,
                                 htlc.Htlc.amount,
                                 outPoint,
                                 keyset.self_revocation_key,
                                 keyset.self_delayed_payment_key,
                                 Context.to_self_delay);
            }

            var htlc_output = TransactionHelper.SeriaizeTransaction(Context.transactionSerializer, Hex.FromString(vectors.HtlcTx[htlcOutputIndex++]));

            var htlc_remote_signature = Context.scripts.SignInput(
               Context.transactionSerializer,
               htlcTransaction,
               Context.remote_htlcsecretkey,
               inputIndex: 0,
               redeemScript: redeemScript,
               htlc.Htlc.amount / 1000);

            var expected_htlc_remote_signature = Hex.ToString(htlc_output.Inputs[0].ScriptWitness.Components[1].RawData.AsSpan());
            Assert.Equal(expected_htlc_remote_signature, Hex.ToString(htlc_remote_signature.GetSpan()));

            var htlc_local_signature = Context.scripts.SignInput(
               Context.transactionSerializer,
               htlcTransaction,
               Context.local_htlcsecretkey,
               inputIndex: 0,
               redeemScript: redeemScript,
               htlc.Htlc.amount / 1000);

            var expected_htlc_local_signature = Hex.ToString(htlc_output.Inputs[0].ScriptWitness.Components[2].RawData.AsSpan());
            Assert.Equal(expected_htlc_local_signature, Hex.ToString(htlc_local_signature.GetSpan()));
         }
      }

      /* BOLT #3:
 *    htlc 5 direction: local->remote
 *    htlc 5 amount_msat: 5000000
 *    htlc 5 expiry: 505
 *    htlc 5 payment_preimage: 0505050505050505050505050505050505050505050505050505050505050505
 *    htlc 6 direction: local->remote
 *    htlc 6 amount_msat: 5000000
 *    htlc 6 expiry: 506
 *    htlc 6 payment_preimage: 0505050505050505050505050505050505050505050505050505050505050505
*/

      public static (List<Htlc>, List<Htlc>) Setup_htlcs_1_5_and_6()
      {
         List<Htlc> htlcs = new List<Htlc>
         {
            new Htlc
            {
               state = htlc_state.RCVD_ADD_ACK_REVOCATION,
               amount = 2000000,
               expirylocktime = 501,
               r = new Preimage(Hex.FromString("0101010101010101010101010101010101010101010101010101010101010101")),
            },

            new Htlc
            {
               state = htlc_state.SENT_ADD_ACK_REVOCATION,
               amount = 5000000,
               expirylocktime = 505,
               r = new Preimage(Hex.FromString("0505050505050505050505050505050505050505050505050505050505050505")),
            },
            new Htlc
            {
               state = htlc_state.SENT_ADD_ACK_REVOCATION,
               amount = 5000000,
               expirylocktime = 506,
               r = new Preimage(Hex.FromString("0505050505050505050505050505050505050505050505050505050505050505")),
            },
         };

         foreach (Htlc htlc in htlcs)
         {
            htlc.rhash = new UInt256(HashGenerator.Sha256(htlc.r));
         }

         var inverted = InvertHtlcs(htlcs);

         return (htlcs, inverted);
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

      public static (List<Htlc>, List<Htlc>) Setup_htlcs_0_to_4()
      {
         List<Htlc> htlcs = new List<Htlc>
         {
            new Htlc
            {
               state = htlc_state.RCVD_ADD_ACK_REVOCATION,
               amount = 1000000,
               expirylocktime = 500,
               r = new Preimage(Hex.FromString("0000000000000000000000000000000000000000000000000000000000000000")),
            },
            new Htlc
            {
               state = htlc_state.RCVD_ADD_ACK_REVOCATION,
               amount = 2000000,
               expirylocktime = 501,
               r = new Preimage(Hex.FromString("0101010101010101010101010101010101010101010101010101010101010101")),
            },
            new Htlc
            {
               state = htlc_state.SENT_ADD_ACK_REVOCATION,
               amount = 2000000,
               expirylocktime = 502,
               r = new Preimage(Hex.FromString("0202020202020202020202020202020202020202020202020202020202020202")),
            },
            new Htlc
            {
               state = htlc_state.SENT_ADD_ACK_REVOCATION,
               amount = 3000000,
               expirylocktime = 503,
               r = new Preimage(Hex.FromString("0303030303030303030303030303030303030303030303030303030303030303")),
            },
            new Htlc
            {
               state = htlc_state.RCVD_ADD_ACK_REVOCATION,
               amount = 4000000,
               expirylocktime = 504,
               r = new Preimage(Hex.FromString("0404040404040404040404040404040404040404040404040404040404040404")),
            },
         };

         foreach (Htlc htlc in htlcs)
         {
            htlc.rhash = new UInt256(HashGenerator.Sha256(htlc.r));
         }

         var inverted = InvertHtlcs(htlcs);

         return (htlcs, inverted);
      }

      /* HTLCs as seen from other side. */

      public static List<Htlc> InvertHtlcs(List<Htlc> htlcs)
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