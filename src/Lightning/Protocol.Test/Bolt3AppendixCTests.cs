using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bitcoin.Primitives.Fundamental;
using Bitcoin.Primitives.Serialization;
using Bitcoin.Primitives.Serialization.Serializers;
using Bitcoin.Primitives.Types;
using Castle.Core.Logging;
using NBitcoin;
using NBitcoin.DataEncoders;
using Protocol.Channels;
using Protocol.Hashing;
using Xunit;
using Block = NBitcoin.Block;
using OutPoint = Bitcoin.Primitives.Types.OutPoint;
using Transaction = NBitcoin.Transaction;

#pragma warning disable IDE1006 // Naming Styles

namespace Protocol.Test
{
   /// <summary>
   /// Tests for https://github.com/lightningnetwork/lightning-rfc/blob/master/03-transactions.md#appendix-b-funding-transaction-test-vectors
   /// </summary>
   public partial class Bolt3AppendixCTests : IClassFixture<Bolt3AppendixCTestContext>
   {
      public Bolt3AppendixCTestContext Context { get; set; }

      public Bolt3AppendixCTests(Bolt3AppendixCTestContext context)
      {
         Context = context;
      }

      [Theory]
      [ClassData(typeof(Bolt3AppendixCTestData))]
      public void Bolt3AppendixC_CommitmentAndHTLCTransaction(Bolt3AppendixCTestData data)
      {
         CommitmenTransactionOut localCommitmenTransactionOut = Context.scripts.CreateCommitmenTransaction(
                  Context.funding_tx_outpoint,
                  Context.funding_amount,
                  Context.local_funding_pubkey,
                  Context.remote_funding_pubkey,
                  LightningScripts.Side.LOCAL,
                  Context.to_self_delay,
                  Context.keyset,
                  data.feerate_per_kw,
                  Context.dust_limit,
                  data.to_local_msat,
                  data.to_remote_msat,
                  data.htlcs_0_to_4.htlcs,
                  Context.commitment_number,
                  Context.cn_obscurer,
                  data.option_anchor_outputs,
                  LightningScripts.Side.LOCAL);

         var remoteCommitmenTransactionOut = Context.scripts.CreateCommitmenTransaction(
                  Context.funding_tx_outpoint,
                  Context.funding_amount,
                  Context.local_funding_pubkey,
                  Context.remote_funding_pubkey,
                  LightningScripts.Side.REMOTE,
                  Context.to_self_delay,
                  Context.keyset,
                  data.feerate_per_kw,
                  Context.dust_limit,
                  data.to_local_msat,
                  data.to_remote_msat,
                  data.htlcs_0_to_4.invertedhtlcs,
                  Context.commitment_number,
                  Context.cn_obscurer,
                  data.option_anchor_outputs,
                  LightningScripts.Side.REMOTE);

         var localTransaction = localCommitmenTransactionOut.Transaction;
         var remoteTransaction = remoteCommitmenTransactionOut.Transaction;

         localTransaction.Hash = Context.transactionHashCalculator.ComputeHash(localTransaction, 1);
         remoteTransaction.Hash = Context.transactionHashCalculator.ComputeHash(remoteTransaction, 1);

         Assert.Equal(localTransaction.Hash, remoteTransaction.Hash);

         // == helper code==
         var output_commit_tx = TransactionHelper.SeriaizeTransaction(Context.transactionSerializer, Hex.FromString(data.output_commit_tx));
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

         Assert.Equal(data.output_commit_tx, Hex.ToString(localTransactionBytes.AsSpan()).Substring(2));

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
                              data.option_anchor_outputs);

               htlcTransaction = Context.scripts.CreateHtlcTimeoutTransaction(
                                 data.option_anchor_outputs,
                                 data.feerate_per_kw,
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
                              data.option_anchor_outputs);

               htlcTransaction = Context.scripts.CreateHtlcSuccessTransaction(
                                 data.option_anchor_outputs,
                                 data.feerate_per_kw,
                                 htlc.Htlc.amount,
                                 outPoint,
                                 keyset.self_revocation_key,
                                 keyset.self_delayed_payment_key,
                                 Context.to_self_delay);
            }

            var htlc_output = TransactionHelper.SeriaizeTransaction(Context.transactionSerializer, Hex.FromString(data.HtlcTx[htlcOutputIndex++]));

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
   }
}

#pragma warning restore IDE1006 // Naming Styles